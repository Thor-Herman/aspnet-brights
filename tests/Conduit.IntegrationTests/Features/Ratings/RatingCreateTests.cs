using System.Net;

using Conduit.Application.Features.Articles.Commands;
using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Features.Ratings.Commands;
using Conduit.Domain.Entities;
using Conduit.IntegrationTests;
using Conduit.Presentation.Controllers;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;
using Xunit.Abstractions;

namespace Conduit.IntegrationTests.Features.Ratings;

public class RatingsTheoryData : TheoryData<NewRatingDto, NewArticleDto, User>
{
    protected static readonly string UserName = "John Doe";
    protected static readonly string Email = "john.doe@gmail.com";
    protected static readonly string Title = "Test Title";
    protected static readonly string Description = "Test Description";
    protected static readonly string Body = "Test Body";
    protected static User GetUser()
    {
        return new User
        {
            Name = UserName,
            Email = Email,
        };
    }

    protected static NewArticleDto GetArticle()
    {
        return new NewArticleDto
        {
            Title = Title,
            Description = Description,
            Body = Body,
        };
    }
}

public class InvalidRatings : RatingsTheoryData
{
    public InvalidRatings()
    {
        Add(new NewRatingDto
        {
            Value = 0,
        },
        GetArticle(), GetUser());

        Add(new NewRatingDto
        {
            Value = 6,
        },
        GetArticle(), GetUser());

        Add(new NewRatingDto
        {
            Description = "I am missing a value",
        }, GetArticle(), GetUser());
    }
}

public class ValidRatings : RatingsTheoryData
{
    public ValidRatings()
    {
        Add(new NewRatingDto
        {
            Value = 1,
            Description = "This article is absolute rubbish!"
        },
        GetArticle(),
        GetUser());

        Add(new NewRatingDto
        {
            Value = 5,
        },
        GetArticle(),
        GetUser());
    }
}

public class RatingCreateTests : TestBase
{
    protected static readonly string ArticleSlug = "test-title";
    public RatingCreateTests(ConduitApiFactory factory, ITestOutputHelper output) : base(factory, output) { }

    [Theory, ClassData(typeof(InvalidRatings))]
    public async Task Cannot_Create_Rating_With_Invalid_Data(NewRatingDto rating, NewArticleDto article, User user)
    {
        await ActingAs(user);
        await Mediator.Send(new NewArticleCommand(article));
        var response = await Act(HttpMethod.Post, $"/articles/{ArticleSlug}/ratings", new NewRatingRequest(rating));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cannot_Create_Rating_To_Non_Existent_Article()
    {
        await ActingAs(new User
        {
            Name = "John Doe",
            Email = "john.doe@gmail.com"
        });

        var response = await Act(HttpMethod.Post, $"/articles/{ArticleSlug}/ratings", new NewRatingRequest(
            new NewRatingDto
            {
                Value = 5,
            }
        ));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Guest_Cannot_Create_Rating()
    {
        var response = await Act(HttpMethod.Post, $"/articles/{ArticleSlug}/ratings", new NewRatingRequest(
            new NewRatingDto
            {
                Value = 5,
            }
        ));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory, ClassData(typeof(ValidRatings))]
    public async Task Can_Create_Rating(NewRatingDto rating, NewArticleDto article, User user)
    {
        await ActingAs(user);
        await Mediator.Send(new NewArticleCommand(article));
        var response = await Act<SingleArticleResponse>(HttpMethod.Post, $"/articles/{ArticleSlug}/ratings", new NewRatingRequest(rating));
        response.Article.Should().BeEquivalentTo(new
        {
            article.Title,
            article.Description,
            article.Body,
            Author = new
            {
                Username = user.Name,
                user.Bio,
                user.Image,
                Following = false,
            },
            RatingsCount = 1,
            TagList = Array.Empty<string>(),
        });
    }

    [Theory, ClassData(typeof(ValidRatings))]
    public async Task Cannot_Create_Existing_Rating(NewRatingDto rating, NewArticleDto article, User user)
    {
        await ActingAs(user);
        await Context.AddAsync(new Article
        {
            Title = article.Title,
            Description = article.Description,
            Body = article.Body,
            Slug = ArticleSlug,
            Author = user,
        });
        await Context.SaveChangesAsync();
        var created = await Context.Articles.FirstOrDefaultAsync(x => x.Slug == ArticleSlug);
        created!.AddRating(user, rating.Value, rating.Description);
        await Context.SaveChangesAsync();
        var response = await Act(HttpMethod.Post, $"/articles/{ArticleSlug}/ratings", new NewRatingRequest(rating));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}