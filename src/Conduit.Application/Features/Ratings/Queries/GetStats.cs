using Conduit.Application.Exceptions;
using Conduit.Application.Interfaces;

using MediatR;

namespace Conduit.Application.Features.Ratings.Queries;

public record RatingGetStatsQuery(string Slug) : IRequest<RatingStatsResponse>;

public record RatingStatsResponse(int Count, double Average, int Median);

public class RatingGetStatsHandler : IRequestHandler<RatingGetStatsQuery, RatingStatsResponse>
{
    private readonly IAppDbContext _dbContext;

    public RatingGetStatsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RatingStatsResponse> Handle(RatingGetStatsQuery request, CancellationToken cancellationToken)
    {
        var article = _dbContext.Articles
            .Where(x => x.Slug == request.Slug)
            .FirstOrDefault() ?? throw new NotFoundException();

        var count = article.UserRatings.Count;
        var average = article.UserRatings.Average(x => x.Rating);
        var median = article.UserRatings.OrderBy(x => x.Rating).Skip(count / 2).First().Rating;

        return Task.FromResult(new RatingStatsResponse(count, average, median));
    }
}
