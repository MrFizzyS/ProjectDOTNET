using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public string CustomerId { get; }
    public string Handle { get; }

    public Author(string customerId, string handle)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainException("Author customer ID is required.");
        }

        if (string.IsNullOrWhiteSpace(handle))
        {
            throw new DomainException("Author handle is required.");
        }

        CustomerId = customerId.Trim();
        Handle = handle.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CustomerId;
        yield return Handle;
    }
}
