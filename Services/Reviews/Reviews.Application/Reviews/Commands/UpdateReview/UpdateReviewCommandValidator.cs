using FluentValidation;
using MongoDB.Bson;
using Reviews.Application.Reviews.Validators;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid MongoDB ObjectId.");
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
        RuleFor(command => command.Body)
            .NotEmpty()
            .MaximumLength(5000);
        RuleFor(command => command.Aspects)
            .SetValidator(new ReviewAspectsDtoValidator()!);
    }
}
