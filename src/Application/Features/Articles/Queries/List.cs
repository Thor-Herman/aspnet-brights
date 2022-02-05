using Application.Extensions;
using Application.Features.Profiles.Queries;
using Application.Interfaces;
using Application.Support;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Articles.Queries;

public class ArticleDTO
{
    public string? Title { get; set; }

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IEnumerable<string>? TagList { get; set; }

    public ProfileDTO? Author { get; set; }

    public bool Favorited { get; set; }

    public int FavoritesCount { get; set; }
}

public record MultipleArticlesResponse(IEnumerable<ArticleDTO> Articles, int ArticlesCount);

public class ArticlesListQuery : PagedQuery, IRequest<MultipleArticlesResponse>
{
    /// <summary>
    /// Filter by author (username)
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Filter by favorites of a user (username)
    /// </summary>
    public string? Favorited { get; set; }

    /// <summary>
    /// Filter by tag
    /// </summary>
    public string? Tag { get; set; }
}

public class ArticlesListHandler : IRequestHandler<ArticlesListQuery, MultipleArticlesResponse>
{
    private readonly IAppDbContextFactory _contextFactory;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public ArticlesListHandler(IAppDbContextFactory contextFactory, IMapper mapper, ICurrentUser currentUser)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<MultipleArticlesResponse> Handle(ArticlesListQuery request, CancellationToken cancellationToken)
    {
        using var contextList = _contextFactory.CreateDbContext();
        using var contextCount = _contextFactory.CreateDbContext();

        var articles = contextList.Articles
            .FilterByRequest(request)
            .OrderByDescending(x => x.Id)
            .ProjectTo<ArticleDTO>(_mapper.ConfigurationProvider, new
            {
                currentUser = _currentUser.User
            })
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var count = contextCount.Articles
            .FilterByRequest(request)
            .CountAsync(cancellationToken);

        return new MultipleArticlesResponse(await articles, await count);
    }
}