
using Conduit.Application.Features.Articles.Commands;
using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Features.Ratings.Commands;
using Conduit.Application.Features.Ratings.Queries;

using MediatR;

using Microsoft.AspNetCore.Authorization;
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

    [HttpGet(Name = "ListArticleRatings")]
    [AllowAnonymous]
    public Task<MultipleRatingsResponse> List(string slug, CancellationToken cancellationToken)
    {
        return _sender.Send(new RatingsListQuery(slug), cancellationToken);
    }

    [HttpPost(Name = "CreateArticleRating")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public Task<SingleArticleResponse> Create(string slug, [FromBody] NewRatingRequest request, CancellationToken cancellationToken)
    {
        return _sender.Send(new RateArticleCommand(slug, request.Rating.Value), cancellationToken);
    }

    [HttpDelete(Name = "DeleteArticleRating")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
    public Task DeleteRating(string slug, CancellationToken cancellationToken)
    {
        return _sender.Send(new RatingDeleteCommand(slug), cancellationToken);
    }

}

public record NewRatingRequest(NewRatingDto Rating);