using Catalog.Bll.Contracts;
using Catalog.Dal.Specifications;
using Catalog.Domain;

namespace Catalog.Bll.Specifications;

/// <summary>Describes filtering, eager includes, sort order, and SQL page window for product search.</summary>
public sealed class ProductCatalogSpecification : Specification<Product>
{
    public ProductCatalogSpecification(ProductQueryDto query)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var productType = query.ProductType;
        var minPrice = query.MinPrice;
        var maxPrice = query.MaxPrice;
        var onlyActive = query.OnlyActive;
        Criteria = product =>
            (!onlyActive || product.IsActive) &&
            (search == null || product.Name.Contains(search) || product.Sku.Contains(search)) &&
            (productType == null || product.ProductType == productType) &&
            (!minPrice.HasValue || product.UnitPrice >= minPrice.Value) &&
            (!maxPrice.HasValue || product.UnitPrice <= maxPrice.Value);

        AddInclude(product => product.GameSystem!);
        var descending = query.Descending;
        switch (query.SortBy.ToLowerInvariant())
        {
            case "sku": SetOrder(product => product.Sku, descending); break;
            case "price": SetOrder(product => product.UnitPrice, descending); break;
            case "createdat": SetOrder(product => product.CreatedAt, descending); break;
            case "producttype": SetOrder(product => product.ProductType, descending); break;
            case "stockquantity": SetOrder(product => product.StockQuantity, descending); break;
            default: SetOrder(product => product.Name, descending); break;
        }

        Skip = (query.Page - 1) * query.PageSize;
        Take = query.PageSize;
    }

    private void SetOrder<TKey>(System.Linq.Expressions.Expression<Func<Product, TKey>> order, bool descending)
    {
        var converted = System.Linq.Expressions.Expression.Lambda<Func<Product, object>>(
            System.Linq.Expressions.Expression.Convert(order.Body, typeof(object)), order.Parameters);
        if (descending) OrderByDescending = converted;
        else OrderBy = converted;
    }
}
