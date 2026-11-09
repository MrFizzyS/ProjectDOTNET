using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Protos;

namespace Reviews.WebApi.GrpcServices;

public sealed class ReviewsGrpcService(ISender sender) : ReviewsGrpc.ReviewsGrpcBase
{
    public override async Task<ReviewMessage> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        if (!ObjectId.TryParse(request.Id, out _))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "id must be a valid ObjectId."));
        }

        var review = await sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);
        return review is null
            ? throw new RpcException(new Status(StatusCode.NotFound, $"Review '{request.Id}' was not found."))
            : ToMessage(review);
    }

    public override async Task<ListReviewsByProductReply> ListReviewsByProduct(
        ListReviewsByProductRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "product_id is required."));
        }

        try
        {
            var page = await sender.Send(
                new GetReviewsListQuery(
                    request.ProductId,
                    request.Page > 0 ? request.Page : 1,
                    request.PageSize > 0 ? request.PageSize : 20),
                context.CancellationToken);

            var reply = new ListReviewsByProductReply
            {
                Page = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            };
            reply.Items.AddRange(page.Items.Select(ToMessage));
            return reply;
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
    }

    private static ReviewMessage ToMessage(ReviewDto r)
    {
        var message = new ReviewMessage
        {
            Id = r.Id.ToString(),
            ProductId = r.ProductId,
            AuthorCustomerId = r.AuthorCustomerId,
            AuthorHandle = r.AuthorHandle,
            Rating = r.Rating,
            Body = r.Body,
            Version = r.Version,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc))
        };

        if (r.ProductName is not null) message.ProductName = r.ProductName;
        if (r.Aspects is not null)
        {
            message.Aspects = new ReviewAspectsMessage
            {
                Usability = r.Aspects.Usability,
                Story = r.Aspects.Story,
                Value = r.Aspects.Value
            };
        }

        message.Replies.AddRange(r.Replies.Select(x => new ReviewReplyMessage
        {
            AuthorCustomerId = x.AuthorCustomerId,
            AuthorHandle = x.AuthorHandle,
            Body = x.Body,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc))
        }));
        return message;
    }
}