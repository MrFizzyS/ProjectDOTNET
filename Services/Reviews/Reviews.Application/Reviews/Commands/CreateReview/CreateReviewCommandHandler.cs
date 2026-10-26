using MediatR;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<CreateReviewCommand, MongoDB.Bson.ObjectId>
{
    public async Task<MongoDB.Bson.ObjectId> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (await reviews.ExistsAsync(
                request.ProductId,
                request.AuthorCustomerId,
                cancellationToken))
        {
            throw new ConflictException("This customer has already reviewed the product.");
        }

        var aspects = request.Aspects is null
            ? null
            : new ReviewAspects(
                request.Aspects.Usability,
                request.Aspects.Story,
                request.Aspects.Value);

        var review = new Review(
            request.ProductId,
            new Author(request.AuthorCustomerId, request.AuthorHandle),
            new ReviewRating(request.Rating),
            request.Body,
            request.ProductName,
            request.OrderId,
            request.PurchaseStatusSnapshot,
            aspects);

        await reviews.AddAsync(review, cancellationToken);
        return review.Id;
    }
}
