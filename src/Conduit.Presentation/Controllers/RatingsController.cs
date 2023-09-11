using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Features.Ratings.Commands;
using Conduit.Application.Features.Ratings.Queries;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Presentation.Controllers;

[Route("articles/{slug}/[controller]")]
[ApiExplorerSettings(GroupName = "Ratings")]
[Authorize]
public class RatingsController
{
    private readonly ISender _sender;

    public RatingsController(ISender sender)
    {
        _sender = sender;
    }


    [HttpGet("stats", Name = "GetArticleRatingStats")]
    [AllowAnonymous]
    public Task<RatingStatsResponse> GetAverage(string slug, CancellationToken cancellationToken)
    {
        return _sender.Send(new RatingGetStatsQuery(slug), cancellationToken);
    }

    [HttpGet(Name = "ListArticleRatings")]
    [AllowAnonymous]
    public Task<MultipleRatingsResponse> List(string slug, CancellationToken cancellationToken)
    {
        return _sender.Send(new RatingsListQuery(slug), cancellationToken);
    }

    [HttpPost(Name = "CreateArticleRating")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SingleArticleResponse>> Create(string slug, [FromBody] NewRatingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _sender.Send(new RateArticleCommand(slug, request.Rating.Value, request.Rating.Description), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new ConflictResult();
        }
    }

    [HttpDelete(Name = "DeleteArticleRating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRating(string slug, CancellationToken cancellationToken)
    {
        await _sender.Send(new RatingDeleteCommand(slug), cancellationToken);
        return new NoContentResult();
    }
}

public record NewRatingRequest(NewRatingDto Rating);