using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Tools.Interfaces;
using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Tools.Seeders
{
    public class ArticlesSeeder : ISeeder
    {
        private readonly IAppDbContext _context;

        public ArticlesSeeder(IAppDbContext context)
        {
            _context = context;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            var users = await _context.Users.ToListAsync(cancellationToken);

            var articles = new Faker<Article>()
                .RuleFor(a => a.Title, f => f.Lorem.Sentence())
                .RuleFor(a => a.Description, f => f.Lorem.Paragraphs(1))
                .RuleFor(a => a.Body, f => f.Lorem.Paragraphs(5))
                .RuleFor(a => a.AuthorId, f => f.PickRandom(users).Id)
                .RuleFor(a => a.CreatedAt, f => f.Date.Recent(90))
                .Generate(500);

            await _context.Articles.AddRangeAsync(articles, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _context.Comments.AddRangeAsync(
                new Faker<Comment>()
                    .RuleFor(a => a.Body, f => f.Lorem.Paragraphs(2))
                    .RuleFor(a => a.ArticleId, f => f.PickRandom(articles).Id)
                    .RuleFor(a => a.AuthorId, f => f.PickRandom(users).Id)
                    .RuleFor(a => a.CreatedAt, f => f.Date.Recent(7))
                    .Generate(5000),
                cancellationToken
            );

            await _context.SaveChangesAsync(cancellationToken);

            var tags = new Faker<Tag>()
                .RuleFor(a => a.Name, f => f.Lorem.Word())
                .Generate(100);

            await _context.Tags.AddRangeAsync(tags, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _context.ArticleTags.AddRangeAsync(
                new Faker<ArticleTag>()
                    .RuleFor(a => a.ArticleId, f => f.PickRandom(articles).Id)
                    .RuleFor(a => a.TagId, f => f.PickRandom(tags).Id)
                    .Generate(1),
                cancellationToken
            );

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}