using FluentValidation;
using Reviews.Application.Reviews.Validators;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(command => command.AuthorCustomerId)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(command => command.AuthorHandle)
            .NotEmpty()
            .MaximumLength(64);
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
        RuleFor(command => command.Body)
            .NotEmpty()
            .MaximumLength(5000);
        RuleFor(command => command.ProductName).MaximumLength(200);
        RuleFor(command => command.OrderId).MaximumLength(100);
        RuleFor(command => command.PurchaseStatusSnapshot).MaximumLength(50);
        RuleFor(command => command.Aspects)
            .SetValidator(new ReviewAspectsDtoValidator()!);
    }
}
