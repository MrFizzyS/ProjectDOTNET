using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed class GetReviewByIdQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewByIdQuery, ReviewDto?>
{
    public Task<ReviewDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
        => queries.GetByIdAsync(ObjectId.Parse(request.Id), cancellationToken);
}
