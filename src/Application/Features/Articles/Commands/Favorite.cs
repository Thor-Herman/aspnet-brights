using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Extensions;
using Application.Features.Articles.Queries;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Articles.Commands
{
    public record ArticleFavoriteCommand(string Slug, bool Favorite) : IAuthorizationRequest<ArticleEnvelope>;

    public class ArticleFavoriteHandler : IAuthorizationRequestHandler<ArticleFavoriteCommand, ArticleEnvelope>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;

        public ArticleFavoriteHandler(IAppDbContext context, IMapper mapper, ICurrentUser currentUser)
        {
            _context = context;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<ArticleEnvelope> Handle(ArticleFavoriteCommand request, CancellationToken cancellationToken)
        {
            var article = await _context.Articles
                .Include(x => x.FavoredUsers)
                .FindAsync(x => x.Slug == request.Slug, cancellationToken);

            if (request.Favorite)
            {
                if (!article.IsFavoritedBy(_currentUser.User))
                {
                    article.FavoredUsers.Add(new ArticleFavorite { User = _currentUser.User });
                }
            }
            else
            {
                if (article.IsFavoritedBy(_currentUser.User))
                {
                    article.FavoredUsers.RemoveAll(x => x.UserId == _currentUser.User.Id);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new ArticleEnvelope(_mapper.Map<ArticleDTO>(article));
        }
    }
}
