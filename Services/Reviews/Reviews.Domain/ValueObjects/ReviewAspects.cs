using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class ReviewAspects : ValueObject
{
    public int Usability { get; }
    public int Story { get; }
    public int Value { get; }

    public ReviewAspects(int usability, int story, int value)
    {
        ValidateScore(usability, nameof(usability));
        ValidateScore(story, nameof(story));
        ValidateScore(value, nameof(value));

        Usability = usability;
        Story = story;
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Usability;
        yield return Story;
        yield return Value;
    }

    private static void ValidateScore(int score, string name)
    {
        if (score is < 1 or > 5)
        {
            throw new DomainException($"The {name} score must be between 1 and 5.");
        }
    }
}
