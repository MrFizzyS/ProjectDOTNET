using MediatR;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed record GetReviewByIdQuery(string Id) : IRequest<ReviewDto?>;
