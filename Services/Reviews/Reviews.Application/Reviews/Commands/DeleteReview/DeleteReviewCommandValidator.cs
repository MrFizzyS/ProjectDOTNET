using FluentValidation;
using MongoDB.Bson;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid MongoDB ObjectId.");
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
