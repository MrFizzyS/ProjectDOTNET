using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public sealed class ReviewReply
{
    public Author Author { get; private set; } = null!;
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private ReviewReply()
    {
    }

    public ReviewReply(Author author, string body, DateTime? createdAt = null)
    {
        Author = author ?? throw new DomainException("Reply author is required.");
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Reply body is required.");
        }

        Body = body.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
