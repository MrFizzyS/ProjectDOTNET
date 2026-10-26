using MediatR;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand(
    string Id,
    int ExpectedVersion,
    int Rating,
    string Body,
    ReviewAspectsDto? Aspects = null) : IRequest;
