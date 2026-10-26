using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class ReviewRating : ValueObject
{
    public int Value { get; }

    public ReviewRating(int value)
    {
        if (value is < 1 or > 5)
        {
            throw new DomainException("A review rating must be between 1 and 5.");
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
