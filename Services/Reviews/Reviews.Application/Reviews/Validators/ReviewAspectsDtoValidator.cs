using FluentValidation;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Validators;

public sealed class ReviewAspectsDtoValidator : AbstractValidator<ReviewAspectsDto>
{
    public ReviewAspectsDtoValidator()
    {
        RuleFor(aspects => aspects.Usability).InclusiveBetween(1, 5);
        RuleFor(aspects => aspects.Story).InclusiveBetween(1, 5);
        RuleFor(aspects => aspects.Value).InclusiveBetween(1, 5);
    }
}
