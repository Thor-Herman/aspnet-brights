using Conduit.Application.Exceptions;
using Conduit.Application.Extensions;
using Conduit.Application.Interfaces;

using MediatR;

namespace Conduit.Application.Features.Ratings.Commands;

public record RatingDeleteCommand(string Slug) : IRequest;

public class RatingDeleteHandler : IRequestHandler<RatingDeleteCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RatingDeleteHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RatingDeleteCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
        var rating = article.UserRatings.FirstOrDefault(x => x.UserId == _currentUser.User!.Id);

        if (rating == null)
            throw new NotFoundException(nameof(rating));
        if (rating.UserId != _currentUser.User!.Id)
            throw new ForbiddenException();

        article.RemoveRating(_currentUser.User!);

        await _context.SaveChangesAsync(cancellationToken);
    }
}