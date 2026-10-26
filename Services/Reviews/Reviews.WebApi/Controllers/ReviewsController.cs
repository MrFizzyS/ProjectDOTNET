using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(ISender sender, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReviewResponse>>> GetList(
        [FromQuery] GetReviewsRequest request,
        CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetReviewsListQuery>(request);
        var result = await sender.Send(query, cancellationToken);
        return Ok(mapper.Map<PagedResult<ReviewResponse>>(result));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetReviewByIdQuery>(id);
        var review = await sender.Send(query, cancellationToken);
        if (review is null)
        {
            throw new NotFoundException($"Review '{id}' was not found.");
        }

        return Ok(mapper.Map<ReviewResponse>(review));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReviewResponse>> Create(
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateReviewCommand>(request);
        var id = await sender.Send(command, cancellationToken);
        var query = mapper.Map<GetReviewByIdQuery>(id.ToString());
        var review = await sender.Send(query, cancellationToken);
        if (review is null)
        {
            throw new InvalidOperationException("The created review could not be read back.");
        }

        var response = mapper.Map<ReviewResponse>(review);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(
        string id,
        UpdateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<UpdateReviewCommand>(request, options => options.Items["Id"] = id);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(
        string id,
        [FromQuery] DeleteReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<DeleteReviewCommand>(request, options => options.Items["Id"] = id);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
