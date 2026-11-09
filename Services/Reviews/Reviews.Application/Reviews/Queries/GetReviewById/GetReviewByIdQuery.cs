using MediatR;
using Platform.ServiceDefaults.Caching;
using Reviews.Application.Caching;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed record GetReviewByIdQuery(string Id) : IRequest<ReviewDto?>, ICacheableQuery
{
    public string CacheKey => ReviewsCacheKeys.Item(Id);
    public TieredCacheEntryOptions CacheOptions => ReviewsCacheKeys.ItemTtl;
}