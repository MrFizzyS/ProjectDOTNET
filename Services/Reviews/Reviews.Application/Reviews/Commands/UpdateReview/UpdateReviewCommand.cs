using MediatR;
using Reviews.Application.Caching;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand(
    string Id,
    int ExpectedVersion,
    int Rating,
    string Body,
    ReviewAspectsDto? Aspects = null) : IRequest, ICacheInvalidatingCommand
{
    public string InvalidationPrefix => ReviewsCacheKeys.Prefix;
    public string InvalidationReason => "review updated";
}