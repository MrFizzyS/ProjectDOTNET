using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(
    string ProductId,
    string AuthorCustomerId,
    string AuthorHandle,
    int Rating,
    string Body,
    string? ProductName = null,
    string? OrderId = null,
    string? PurchaseStatusSnapshot = null,
    ReviewAspectsDto? Aspects = null) : IRequest<ObjectId>;
