using MongoDB.Bson;

namespace Reviews.Application.Common.Models;

public sealed class ReviewDto
{
    public ObjectId Id { get; set; }
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

public sealed class ReviewAspectsDto
{
    public int Usability { get; set; }
    public int Story { get; set; }
    public int Value { get; set; }
}

public sealed class ReviewReplyDto
{
    public string AuthorCustomerId { get; set; } = string.Empty;
    public string AuthorHandle { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
