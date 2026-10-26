using FluentValidation;
using MongoDB.Bson;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid MongoDB ObjectId.");
    }
}
