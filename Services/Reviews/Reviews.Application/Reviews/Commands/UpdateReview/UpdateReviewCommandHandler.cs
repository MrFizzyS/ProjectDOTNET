using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<UpdateReviewCommand>
{
    public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Review '{request.Id}' was not found.");

        if (review.Version != request.ExpectedVersion)
        {
            throw new ConflictException("The review has changed since it was read.");
        }

        var aspects = request.Aspects is null
            ? null
            : new ReviewAspects(
                request.Aspects.Usability,
                request.Aspects.Story,
                request.Aspects.Value);

        review.UpdateContent(new ReviewRating(request.Rating), request.Body, aspects);

        if (!await reviews.UpdateAsync(review, request.ExpectedVersion, cancellationToken))
        {
            throw new ConflictException("The review has changed since it was read.");
        }
    }
}
