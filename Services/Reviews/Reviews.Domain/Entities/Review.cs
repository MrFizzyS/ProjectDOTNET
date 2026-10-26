using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public sealed class Review : BaseEntity
{
    public string ProductId { get; private set; } = string.Empty;
    public string? ProductName { get; private set; }
    public Author Author { get; private set; } = null!;
    public string? OrderId { get; private set; }
    public string? PurchaseStatusSnapshot { get; private set; }
    public ReviewRating Rating { get; private set; } = null!;
    public ReviewAspects? Aspects { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public IReadOnlyCollection<ReviewReply> Replies { get; private set; } = Array.Empty<ReviewReply>();

    private Review()
    {
    }

    public Review(
        string productId,
        Author author,
        ReviewRating rating,
        string body,
        string? productName = null,
        string? orderId = null,
        string? purchaseStatusSnapshot = null,
        ReviewAspects? aspects = null)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new DomainException("Product ID is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Review body is required.");
        }

        ProductId = productId.Trim();
        Author = author ?? throw new DomainException("Review author is required.");
        Rating = rating ?? throw new DomainException("Review rating is required.");
        Body = body.Trim();
        ProductName = NormalizeOptional(productName);
        OrderId = NormalizeOptional(orderId);
        PurchaseStatusSnapshot = NormalizeOptional(purchaseStatusSnapshot);
        Aspects = aspects;
    }

    public void UpdateContent(ReviewRating rating, string body, ReviewAspects? aspects = null)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Review body is required.");
        }

        Rating = rating ?? throw new DomainException("Review rating is required.");
        Body = body.Trim();
        Aspects = aspects;
        Touch();
    }

    public void AddReply(ReviewReply reply)
    {
        if (reply is null)
        {
            throw new DomainException("Reply is required.");
        }

        Replies = Replies.Append(reply).ToArray();
        Touch();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
