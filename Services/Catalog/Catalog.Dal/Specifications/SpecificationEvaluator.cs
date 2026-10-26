using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(IQueryable<TEntity> inputQuery, ISpecification<TEntity> specification) where TEntity : class
    {
        var query = ApplyCriteriaAndIncludes(inputQuery, specification);
        if (specification.OrderBy is not null) query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null) query = query.OrderByDescending(specification.OrderByDescending);
        if (specification.Skip.HasValue) query = query.Skip(specification.Skip.Value);
        if (specification.Take.HasValue) query = query.Take(specification.Take.Value);
        return query;
    }

    public static IQueryable<TEntity> GetCountQuery<TEntity>(IQueryable<TEntity> inputQuery, ISpecification<TEntity> specification) where TEntity : class
        => specification.Criteria is null ? inputQuery : inputQuery.Where(specification.Criteria);

    private static IQueryable<TEntity> ApplyCriteriaAndIncludes<TEntity>(IQueryable<TEntity> query, ISpecification<TEntity> specification) where TEntity : class
    {
        if (specification.Criteria is not null) query = query.Where(specification.Criteria);
        foreach (var include in specification.Includes) query = query.Include(include);
        return query;
    }
}
