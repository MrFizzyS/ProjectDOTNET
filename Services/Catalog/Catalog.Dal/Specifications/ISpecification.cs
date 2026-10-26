using System.Linq.Expressions;

namespace Catalog.Dal.Specifications;

public interface ISpecification<TEntity> where TEntity : class
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; }
    Expression<Func<TEntity, object>>? OrderBy { get; }
    Expression<Func<TEntity, object>>? OrderByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
}

public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    private readonly List<Expression<Func<TEntity, object>>> _includes = [];
    public Expression<Func<TEntity, bool>>? Criteria { get; protected set; }
    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes;
    public Expression<Func<TEntity, object>>? OrderBy { get; protected set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; protected set; }
    public int? Skip { get; protected set; }
    public int? Take { get; protected set; }

    protected void AddInclude(Expression<Func<TEntity, object>> include) => _includes.Add(include);
}
