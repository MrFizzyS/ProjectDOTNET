using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed class GetReviewsListQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    public Task<PagedResult<ReviewDto>> Handle(
        GetReviewsListQuery request,
        CancellationToken cancellationToken)
        => queries.GetListAsync(
            request.ProductId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
}
