using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Review '{request.Id}' was not found.");

        if (review.Version != request.ExpectedVersion)
        {
            throw new ConflictException("The review has changed since it was read.");
        }

        if (!await reviews.DeleteAsync(id, request.ExpectedVersion, cancellationToken))
        {
            throw new ConflictException("The review has changed since it was read.");
        }
    }
}
