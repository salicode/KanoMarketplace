
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KanoMarketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok();

    [HttpGet("operations-map")]
    public async Task<IActionResult> OperationsMap() => Ok();

    [HttpPut("merchants/{id:guid}/approve")]
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

    [HttpGet("audit-logs")]


    public async Task<IActionResult> AuditLogs() => Ok();

    public record SetUserRoleBody(string PhoneNumber, string Role, string? FullName);

    private static readonly Dictionary<string, int> Roles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = 0,
        ["Merchant"] = 1,
        ["Rider"] = 2,
        ["CustomerSupport"] = 3,
        ["OperationsManager"] = 4,
        ["FinanceAdmin"] = 5,
        ["SuperAdmin"] = 6
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
}