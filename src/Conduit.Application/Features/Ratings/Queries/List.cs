using Conduit.Application.Extensions;
using Conduit.Application.Features.Auth.Queries;
using Conduit.Application.Features.Profiles.Queries;
using Conduit.Application.Interfaces;
using Conduit.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Conduit.Application.Features.Ratings.Queries;
public class RatingDto
{
    public required int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required ProfileDto Author { get; set; }

}
public static class RatingDtoMapper
{
    public static RatingDto Map(ArticleRating rating, User? user)
    {
        return new()
        {
            Value = rating.Rating,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt,
            Author = rating.User.MapToProfile(user),
        };
    }
}
public record MultipleRatingsResponse(IEnumerable<RatingDto> Ratings);
public record RatingsListQuery(string Slug) : IRequest<MultipleRatingsResponse>;

public class RatingsListHandler : IRequestHandler<RatingsListQuery, MultipleRatingsResponse>
{
    private readonly IAppDbContext _context;

    public RatingsListHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<MultipleRatingsResponse> Handle(RatingsListQuery request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
        var ratings = article.UserRatings.Select(x => RatingDtoMapper.Map(x, x.User));
        return new MultipleRatingsResponse(ratings);
    }
}