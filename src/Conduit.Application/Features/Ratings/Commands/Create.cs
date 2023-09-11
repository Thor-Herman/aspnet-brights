using Conduit.Application.Extensions;
using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Interfaces;

using FluentValidation;

using MediatR;

namespace Conduit.Application.Features.Ratings.Commands;

public class NewRatingDto
{
    public int Value { get; set; }
    public string Description { get; set; } = "";
}
public class RatingCreateValidator : AbstractValidator<RateArticleCommand>
{
    public RatingCreateValidator()
    {
        RuleFor(x => x.Rating).NotNull().NotEmpty().InclusiveBetween(1, 5);
    }
}
public record RateArticleCommand(string Slug, int Rating, string Description) : IRequest<SingleArticleResponse>;

public class ArticleRateHandler : IRequestHandler<RateArticleCommand, SingleArticleResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ArticleRateHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SingleArticleResponse> Handle(RateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles
            .FindAsync(x => x.Slug == request.Slug, cancellationToken);

        if (article.UserRatings.Any(a => a.UserId == _currentUser.User!.Id))
            throw new InvalidOperationException("You have already rated this article.");

        article.AddRating(_currentUser.User!, request.Rating, request.Description);

        await _context.SaveChangesAsync(cancellationToken);

        return new SingleArticleResponse(article.Map(_currentUser.User));
    }
}