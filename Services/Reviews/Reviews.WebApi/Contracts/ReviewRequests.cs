using Reviews.Application.Common.Models;

namespace Reviews.WebApi.Contracts;

public sealed class CreateReviewRequest
{
    public string ProductId { get; init; } = string.Empty;
    public string AuthorCustomerId { get; init; } = string.Empty;
    public string AuthorHandle { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? ProductName { get; init; }
    public string? OrderId { get; init; }
    public string? PurchaseStatusSnapshot { get; init; }
    public ReviewAspectsDto? Aspects { get; init; }
}

public sealed class UpdateReviewRequest
{
    public int ExpectedVersion { get; init; }
    public int Rating { get; init; }
    public string Body { get; init; } = string.Empty;
    public ReviewAspectsDto? Aspects { get; init; }
}

public sealed class DeleteReviewRequest
{
    public int ExpectedVersion { get; init; }
}

public sealed class GetReviewsRequest
{
    public string? ProductId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class ReviewResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string AuthorCustomerId { get; set; } = string.Empty;
    public string AuthorHandle { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? PurchaseStatusSnapshot { get; set; }
    public int Rating { get; set; }
    public ReviewAspectsDto? Aspects { get; set; }
    public string Body { get; set; } = string.Empty;
    public IReadOnlyCollection<ReviewReplyDto> Replies { get; set; } = Array.Empty<ReviewReplyDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Version { get; set; }
}
