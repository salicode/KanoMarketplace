// Domain/Entities/Category.cs
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = default!;
    public Guid? ParentCategoryId { get; set; } // supports subcategories; null = top-level
    public bool IsActive { get; set; } = true;
}