using MediatR;
using Reviews.Application.Caching;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed record DeleteReviewCommand(string Id, int ExpectedVersion) : IRequest, ICacheInvalidatingCommand
{
    public string InvalidationPrefix => ReviewsCacheKeys.Prefix;
    public string InvalidationReason => "review deleted";
}