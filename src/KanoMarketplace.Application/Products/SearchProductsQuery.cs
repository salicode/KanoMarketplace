
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Products;

public record ProductSummaryDto(Guid Id, string Name, decimal BasePrice, Guid MerchantId, Guid CategoryId);

public record SearchProductsQuery(string? Search, Guid? CategoryId, int Page = 1, int PageSize = 20)
    : IRequest<List<ProductSummaryDto>>;

public class SearchProductsHandler : IRequestHandler<SearchProductsQuery, List<ProductSummaryDto>>
{
   private readonly IAppDbContext _db;
    public SearchProductsHandler(IAppDbContext db) => _db = db;
public async Task<List<ProductSummaryDto>> Handle(SearchProductsQuery q, CancellationToken ct)
{
    var query = _db.Products.Where(p => p.IsActive).AsQueryable();

    if (q.CategoryId is not null)
        query = query.Where(p => p.CategoryId == q.CategoryId);

    if (!string.IsNullOrWhiteSpace(q.Search))
    {
        var search = q.Search.ToLower();
        query = query.Where(p => p.Name.ToLower().Contains(search));
    }

    return await query
        .OrderBy(p => p.Name)
        .Skip((q.Page - 1) * q.PageSize)
        .Take(q.PageSize)
        .Select(p => new ProductSummaryDto(p.Id, p.Name, p.BasePrice, p.MerchantId, p.CategoryId))
        .ToListAsync(ct);
}
}