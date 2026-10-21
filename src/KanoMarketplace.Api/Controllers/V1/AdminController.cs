
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KanoMarketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;
    public AdminController(IMediator mediator, AppDbContext db) { _mediator = mediator; _db = db; }

    private async Task<T> Scalar<T>(FormattableString sql, CancellationToken ct) =>
        (await _db.Database.SqlQuery<T>(sql).ToListAsync(ct)).FirstOrDefault()!;

    // ---------------- Dashboard ----------------
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        // Order status numbers: 4-12 = in progress, 13 = Delivered, 14 = Cancelled
        return Ok(new
        {
            totalUsers = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Users""", ct),
            customers = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Users"" WHERE ""Role"" = 0", ct),
            merchantsApproved = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Merchants"" WHERE ""Status"" = 1", ct),
            merchantsPending = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Merchants"" WHERE ""Status"" = 0", ct),
            riders = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""RiderProfiles""", ct),
            ridersOnline = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""RiderProfiles"" WHERE ""IsOnline""", ct),
            ordersTotal = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Orders""", ct),
            ordersActive = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Orders"" WHERE ""Status"" BETWEEN 4 AND 12", ct),
            ordersDelivered = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Orders"" WHERE ""Status"" = 13", ct),
            ordersCancelled = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Orders"" WHERE ""Status"" = 14", ct),
            deliveredRevenue = await Scalar<decimal>($@"SELECT COALESCE(SUM(""Total""), 0) AS ""Value"" FROM ""Orders"" WHERE ""Status"" = 13", ct)
        });
    }

    // ---------------- Operations map (online riders with positions) ----------------
    public class RiderPoint
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime? LastLocationAt { get; set; }
    }

    [HttpGet("operations-map")]
    public async Task<IActionResult> OperationsMap(CancellationToken ct)
    {
        var riders = await _db.Database.SqlQuery<RiderPoint>($@"
            SELECT ""Id"", ""UserId"",
                   ST_Y(""CurrentLocation""::geometry) AS ""Lat"",
                   ST_X(""CurrentLocation""::geometry) AS ""Lng"",
                   ""LastLocationAt""
            FROM ""RiderProfiles""
            WHERE ""IsOnline"" AND ""CurrentLocation"" IS NOT NULL").ToListAsync(ct);

        var activeOrders = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Orders"" WHERE ""Status"" BETWEEN 4 AND 12", ct);
        return Ok(new { activeOrders, onlineRiders = riders.Count, riders });
    }

    // ---------------- Merchants ----------------
    [HttpPut("merchants/{id:guid}/approve")]
    public async Task<IActionResult> ApproveMerchant(Guid id, CancellationToken ct)
    {
        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Merchants"" SET ""Status"" = 1, ""UpdatedAt"" = now() WHERE ""Id"" = {id}", ct);
        if (n == 0) return NotFound(new { error = "Merchant not found." });

        // give the owner the Merchant role (only upgrades customers, never changes staff roles)
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""Role"" = 1
            WHERE ""Id"" = (SELECT ""OwnerUserId"" FROM ""Merchants"" WHERE ""Id"" = {id}) AND ""Role"" = 0", ct);
        return Ok(new { merchantId = id, status = "Approved" });
    }

    [HttpPut("merchants/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendMerchant(Guid id, CancellationToken ct)
    {
        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Merchants"" SET ""Status"" = 2, ""UpdatedAt"" = now() WHERE ""Id"" = {id}", ct);
        return n == 0 ? NotFound(new { error = "Merchant not found." }) : Ok(new { merchantId = id, status = "Suspended" });
    }

    // ---------------- Riders (id = the rider's USER id) ----------------
    [HttpPut("riders/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRider(Guid id, CancellationToken ct)
    {
        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""Role"" = 2 WHERE ""Id"" = {id} AND ""Role"" IN (0, 2)", ct);
        if (n == 0) return NotFound(new { error = "User not found, or the user already has another role." });

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""RiderProfiles"" (""Id"", ""UserId"", ""IsOnline"", ""CreatedAt"")
            SELECT gen_random_uuid(), {id}, false, now()
            WHERE NOT EXISTS (SELECT 1 FROM ""RiderProfiles"" WHERE ""UserId"" = {id})", ct);
        return Ok(new { userId = id, role = "Rider" });
    }

    // ---------------- Audit log (order status changes) ----------------
    public class AuditRow
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public int FromStatus { get; set; }
        public int ToStatus { get; set; }
        public string? ChangedBy { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(CancellationToken ct)
    {
        var rows = await _db.Database.SqlQuery<AuditRow>($@"
            SELECT ""Id"", ""OrderId"", ""FromStatus"", ""ToStatus"", ""ChangedBy"", ""Reason"", ""CreatedAt""
            FROM ""OrderStatusHistories""
            ORDER BY ""CreatedAt"" DESC
            LIMIT 100").ToListAsync(ct);
        return Ok(rows);
    }

    // ---------------- Create a user with a role, or change a role ----------------
    public record SetUserRoleBody(string PhoneNumber, string Role, string? FullName);

    private static readonly Dictionary<string, int> Roles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = 0, ["Merchant"] = 1, ["Rider"] = 2, ["CustomerSupport"] = 3,
        ["OperationsManager"] = 4, ["FinanceAdmin"] = 5, ["SuperAdmin"] = 6
    };

    [HttpPost("users")]
    public async Task<IActionResult> SetUserRole(SetUserRoleBody body, CancellationToken ct)
    {
        if (!Roles.TryGetValue(body.Role, out var role))
            return BadRequest(new { error = "Role must be one of: " + string.Join(", ", Roles.Keys) });

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""Users"" (""Id"", ""PhoneNumber"", ""FullName"", ""Role"", ""IsActive"", ""CreatedAt"")
            SELECT gen_random_uuid(), {body.PhoneNumber}, {body.FullName}, {role}, true, now()
            WHERE NOT EXISTS (SELECT 1 FROM ""Users"" WHERE ""PhoneNumber"" = {body.PhoneNumber})", ct);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""Role"" = {role} WHERE ""PhoneNumber"" = {body.PhoneNumber}", ct);

        return Ok(new { phoneNumber = body.PhoneNumber, role = body.Role });
    }

        // =====================================================================
    //  USER MANAGEMENT
    // =====================================================================
    private static readonly string[] RoleNames =
        { "Customer", "Merchant", "Rider", "CustomerSupport", "OperationsManager", "FinanceAdmin", "SuperAdmin" };

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value, out var g) ? g : null;

    public class UserRow
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public int Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private static object ToDto(UserRow u) => new
    {
        u.Id, u.PhoneNumber, u.Email, u.FullName,
        role = u.Role >= 0 && u.Role < RoleNames.Length ? RoleNames[u.Role] : u.Role.ToString(),
        u.IsActive, u.CreatedAt
    };

    // GET admin/users?page=1&pageSize=20&role=Rider&q=0801&isActive=true
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        int page = 1, int pageSize = 20, string? role = null, string? q = null, bool? isActive = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        int? roleNum = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Roles.TryGetValue(role, out var r))
                return BadRequest(new { error = "Role must be one of: " + string.Join(", ", Roles.Keys) });
            roleNum = r;
        }
        string? pattern = string.IsNullOrWhiteSpace(q) ? null : "%" + q.Trim() + "%";

        var total = await Scalar<int>($@"
            SELECT COUNT(*)::int AS ""Value"" FROM ""Users""
            WHERE ({roleNum}::int IS NULL OR ""Role"" = {roleNum}::int)
              AND ({isActive}::boolean IS NULL OR ""IsActive"" = {isActive}::boolean)
              AND ({pattern}::text IS NULL OR ""PhoneNumber"" ILIKE {pattern}::text
                   OR ""FullName"" ILIKE {pattern}::text OR ""Email"" ILIKE {pattern}::text)", ct);

        var rows = await _db.Database.SqlQuery<UserRow>($@"
            SELECT ""Id"", ""PhoneNumber"", ""Email"", ""FullName"", ""Role"", ""IsActive"", ""CreatedAt""
            FROM ""Users""
            WHERE ({roleNum}::int IS NULL OR ""Role"" = {roleNum}::int)
              AND ({isActive}::boolean IS NULL OR ""IsActive"" = {isActive}::boolean)
              AND ({pattern}::text IS NULL OR ""PhoneNumber"" ILIKE {pattern}::text
                   OR ""FullName"" ILIKE {pattern}::text OR ""Email"" ILIKE {pattern}::text)
            ORDER BY ""CreatedAt"" DESC
            LIMIT {pageSize} OFFSET {offset}").ToListAsync(ct);

        return Ok(new
        {
            page, pageSize, total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items = rows.Select(ToDto)
        });
    }

    public class MerchantBrief
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = "";
        public int Status { get; set; }
    }

    public class RiderBrief
    {
        public Guid Id { get; set; }
        public bool IsOnline { get; set; }
        public string? VehicleType { get; set; }
        public DateTime? LastLocationAt { get; set; }
    }

    // GET admin/users/{id}
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = (await _db.Database.SqlQuery<UserRow>($@"
            SELECT ""Id"", ""PhoneNumber"", ""Email"", ""FullName"", ""Role"", ""IsActive"", ""CreatedAt""
            FROM ""Users"" WHERE ""Id"" = {id}").ToListAsync(ct)).FirstOrDefault();
        if (user is null) return NotFound(new { error = "User not found." });

        var merchants = await _db.Database.SqlQuery<MerchantBrief>($@"
            SELECT ""Id"", ""BusinessName"", ""Status"" FROM ""Merchants"" WHERE ""OwnerUserId"" = {id}").ToListAsync(ct);
        var rider = (await _db.Database.SqlQuery<RiderBrief>($@"
            SELECT ""Id"", ""IsOnline"", ""VehicleType"", ""LastLocationAt"" FROM ""RiderProfiles"" WHERE ""UserId"" = {id}")
            .ToListAsync(ct)).FirstOrDefault();
        var orders = await Scalar<int>($@"SELECT COUNT(*)::int AS ""Value"" FROM ""Orders"" WHERE ""CustomerId"" = {id}", ct);

        return Ok(new { user = ToDto(user), ordersPlaced = orders, merchants, riderProfile = rider });
    }

    // PUT admin/users/{id}   (edit name / email)
    public record AdminUpdateUserBody(string? FullName, string? Email);

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, AdminUpdateUserBody body, CancellationToken ct)
    {
        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users""
            SET ""FullName"" = COALESCE({body.FullName}::text, ""FullName""),
                ""Email"" = COALESCE({body.Email}::text, ""Email""),
                ""UpdatedAt"" = now()
            WHERE ""Id"" = {id}", ct);
        return n == 0 ? NotFound(new { error = "User not found." }) : NoContent();
    }

    // PUT admin/users/{id}/role
    public record ChangeRoleBody(string Role);

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleBody body, CancellationToken ct)
    {
        if (!Roles.TryGetValue(body.Role, out var role))
            return BadRequest(new { error = "Role must be one of: " + string.Join(", ", Roles.Keys) });
        if (id == CurrentUserId && role != 6)
            return BadRequest(new { error = "You cannot remove your own SuperAdmin role." });

        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""Role"" = {role}, ""UpdatedAt"" = now() WHERE ""Id"" = {id}", ct);
        if (n == 0) return NotFound(new { error = "User not found." });

        // force a fresh login so the new role is placed in their token
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""RefreshTokens"" SET ""Revoked"" = true WHERE ""UserId"" = {id}", ct);
        return Ok(new { userId = id, role = RoleNames[role] });
    }

    // PUT admin/users/{id}/deactivate  and  /activate
    [HttpPut("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct)
    {
        if (id == CurrentUserId) return BadRequest(new { error = "You cannot deactivate your own account." });

        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""IsActive"" = false, ""UpdatedAt"" = now() WHERE ""Id"" = {id}", ct);
        if (n == 0) return NotFound(new { error = "User not found." });

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""RefreshTokens"" SET ""Revoked"" = true WHERE ""UserId"" = {id}", ct);
        return Ok(new { userId = id, isActive = false });
    }

    [HttpPut("users/{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id, CancellationToken ct)
    {
        var n = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""IsActive"" = true, ""UpdatedAt"" = now() WHERE ""Id"" = {id}", ct);
        return n == 0 ? NotFound(new { error = "User not found." }) : Ok(new { userId = id, isActive = true });
    }
}