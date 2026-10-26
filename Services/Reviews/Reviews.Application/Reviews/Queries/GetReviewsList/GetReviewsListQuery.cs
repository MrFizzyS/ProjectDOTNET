using MediatR;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed record GetReviewsListQuery(
    string? ProductId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ReviewDto>>;
