using AutoMapper;
using MongoDB.Bson;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi;

public sealed class ReviewsMappingProfile : Profile
{
    public ReviewsMappingProfile()
    {
        CreateMap<CreateReviewRequest, CreateReviewCommand>();
        CreateMap<UpdateReviewRequest, UpdateReviewCommand>()
            .ForCtorParam(nameof(UpdateReviewCommand.Id), options => options.MapFrom((_, context) => context.Items["Id"]));
        CreateMap<DeleteReviewRequest, DeleteReviewCommand>()
            .ForCtorParam(nameof(DeleteReviewCommand.Id), options => options.MapFrom((_, context) => context.Items["Id"]));
        CreateMap<GetReviewsRequest, GetReviewsListQuery>();
        CreateMap<string, Reviews.Application.Reviews.Queries.GetReviewById.GetReviewByIdQuery>()
            .ConstructUsing(id => new(id));
        CreateMap<ReviewDto, ReviewResponse>();
        CreateMap<PagedResult<ReviewDto>, PagedResult<ReviewResponse>>();
        CreateMap<ObjectId, string>().ConvertUsing(id => id.ToString());
    }
}
