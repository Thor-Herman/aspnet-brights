Ratings System
===
This repo is a fork of [Realworld app](https://github.com/adr1enbe4udou1n/aspnetcore-realworld-example-app) intended for a brights course on dotnet. 

## Table of Contents
- [Project Setup](project-setup)
  - [Prerequisities](prerequisites)
  - [Start the app](start-the-app)
- [Data Model](data-model)
  - [Rating Model](rating-model)
  - [Relations](relations)
  - [Migrations](migrations)
- [Controller](controller)
  - [End Points](end-points)
  - [Create Rating](create-ratings)
  - [List Ratings](list-ratings)
  - [Delete Rating](delete-ratings)
  - [Data Transfer Objects](data-transfer-objects)
- [Further Work](further-work)

## Project Setup

### Prerequisities
Ensure you have the following dependencies installed:
- [.NET 7.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) 
- [Docker](https://docs.docker.com/get-docker/) 

If you wish to inspect the database as well, you can download
- [pgadmin](https://www.pgadmin.org/download/)

### Start the app 
This project uses PostgreSQL for its database. To start the database, simply do `docker-compose up -d` from the project folder. 

To populate the database with sample data (seeding), run `make seed`. This is only necessary the first time you set up the program. 

Finally, to start the server, use `make run`

Now you should be able to access swagger and inspect the existing API at http://localhost:5000/api

Start by reviewing the existing methods and endpoint in Swagger, and then try to find their implementations in the code. Become familiar with the project architecture and try to determine the patterns used. Once comfortable with the solution, move on to the data model. 
<details closed>
    <summary><b>Explanation</b></summary>
    The solution (<b>Realworld.sln</b>) is divided into several smaller projects that handle different responsibilities<br/>
    <ul>
        <li><i>Conduit.Application</i> contains the commands and queries that define the functionality. All interfaces can be found here too.</li>
        <li><i>Conduit.Domain</i> has the definitions for the database entities</li>
        <li><i>Conduit.Presentation</i> defines the end points and controllers that handle incoming requests. It also sets up Swagger in the ServiceExtension </li>
        <li><i>Conduit.Infrastructure</i> contains the DbContext and utility classes such as a PasswordHasher</li>
        <li><i>Conduit.WebUI</i> is the entry point of the application. The `Program.cs` is located here together with various application settings.</li>
    </ul>
    The main pattern used is CQRS, which you can read more about <a href="https://code-maze.com/cqrs-mediatr-fluentvalidation/" target="_blank">here</a>
</details>
<br/>

## Data Model
The first part of the project is to modify the existing data model to allow for storing ratings. A user should be able to review each article and give a rating between 1 and 5 to each one. They cannot have more than one review per article. There are several ways you could implement this, start by thinking of how you would structure the relational database. 
<details closed>
    <summary><b>Possible Solution</b></summary>
    <img src="https://i.imgur.com/KkV8R5Z.png" w="100" h="auto" />
    Each rating has a foreign key to article and user. Together they form the key for the entity. Additionally, an integer field allows for a rating value. 
</details>


### Rating Model
To add a new table to the database, you will have to create a new entity using Entity Framework. Define your entity class in the correct folder with the necessary properties. 

<details closed>
    <summary><b>Solution</b></summary>
    Here is a possible solution that implements the relation presented previously. 
    <pre><span style="color: green;">namespace Conduit.Domain.Entities;<br/>
public class ArticleRating
{
    public int ArticleId { get; set; }
    public virtual required Article Article { get; set; }
    public int UserId { get; set; }
    public virtual required User User { get; set; }
    public int Rating { get; set; }
}</span></pre>
</details>

### Relations
With the class and its properties defined, its relations to other entites must also be declared. Set up the necessary relations in the *DbContext*. 

<details closed>
    <summary><b>Solution</b></summary>
    Here is a possible solution that implements the relation presented previously. <br/>In <i>AppDbContext.cs</i>:
    <pre><span style="color: green;">
    ...<br/>
    modelBuilder.Entity<ArticleRating>(b =>
    {
        b.HasKey(e => new { e.ArticleId, e.UserId });
        b.HasOne(e => e.Article)
            .WithMany(e => e.UserRatings)
            .HasForeignKey(e => e.ArticleId);
        b.HasOne(e => e.User)
            .WithMany(e => e.ArticleRatings)
            .HasForeignKey(e => e.UserId);
    });</span>
</pre>
    In <i>User.cs</i>
    <pre>    
    ...
    private readonly List<ArticleFavorite> _favoriteArticles = new();
    <span style="color: green;">private readonly List<ArticleRating> _articleRatings = new();</span>
    ...
    public virtual IReadOnlyCollection<ArticleFavorite> FavoriteArticles => _favoriteArticles;
    <span style="color: green;">public virtual IReadOnlyCollection<ArticleRating> ArticleRatings => _articleRatings;</span>
</pre>
    In <i>Article.cs</i>
    <pre>
    ...
    private readonly List<ArticleTag> _tags = new();
    private readonly List<ArticleFavorite> _favoredUsers = new();
    <span style="color: green;">private readonly List<ArticleRating> _userRatings = new();</span>
    ...
    public virtual IReadOnlyCollection<ArticleFavorite> FavoredUsers => _favoredUsers;
    <span style="color: green;">public virtual IReadOnlyCollection<ArticleRating> UserRatings => _userRatings;</span>
    ...
    <span style="color: green;">public void RemoveRating(User user) => _userRatings.RemoveAll(x => x.UserId == user.Id);</span>
    <span style="color: green;">public void AddRating(User user, int rating) => _userRatings.Add(new ArticleRating { User = user, Article = this, Rating = rating });</span>
</pre>
</details>

### Migrations
To apply the changes in the data model to the PostgreSQL database, you will have to create a migration. You can use the `dotnet ef` tool for this. To verify that the migration worked as intended, you can use a tool like pgadmin or the postgres CLI to inspect the database.
<details closed>
    <summary><b>Solution</b></summary>
    Perform the following commands in the <i>Conduit.Infrastructure</i> folder:
    <pre>
    $ dotnet clean
    $ dotnet build
    $ dotnet ef migrations add "<Name of your migration>" -s ../Conduit.WebUI
    $ dotnet ef database update -s ../Conduit.WebUI</pre>
</details>
<br/>
    
## Controller
Now that we have updated our data model, it´s time to move on to the controller responsible for handling rating requests. Start by creating a new controller in the correct folder. 

### End Points
Before we start implementing the desired rating functionality, we should ensure that our desired endpoints are reachable. Thus, you should start by creating methods handling the http calls. You can start with returning 200 response codes. 

The controller should support the following methods:
* POST
* GET
* DELETE

At the url `http://localhost:5000/api/articles/{article_slug}/ratings`

<details closed>
    <summary><b>Solution</b></summary>
    <i>In RatingsController.cs</i>
    <pre><span style="color: green;">
using Conduit.Application.Features.Ratings.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;<br/>
namespace Conduit.Presentation.Controllers;<br/>
[Route("articles/{slug}/[controller]")]
[ApiExplorerSettings(GroupName = "Ratings")]
[Authorize]
public class RatingsController
{<br/>
    [HttpGet(Name = "ListArticleRatings")]
    public IActionResult List()
    {
        return new OkObjectResult("List");
    }<br/>
    [HttpPost(Name = "CreateArticleRating")]
    public IActionResult Create()
    {
        return new OkObjectResult("Create");
    }<br/>
    [HttpDelete(Name = "DeleteArticleRating")]
    public IActionResult DeleteRating()
    {
        return new OkObjectResult("Delete");
    }
}</span></pre>
</details>

### Create Rating
Now that we have a skeleton for the incoming requests, it´s time to implement the actual logic. The first step is to define the create method. Create the necessary commands and update the controller with new logic to support the creation of new ratings. 

> **N.B.** Remember to validate the rating! It should only allow integers between 1 and 5.

<details closed>
    <summary><b>Solution</b></summary>
    In <i>RatingsController.cs</i>
    <pre><span style="color: green;">using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Features.Ratings.Commands;
using MediatR;</span>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
<span style="color: green;">using Microsoft.AspNetCore.Http;</span>
...
public class RatingsController
{
    <span style="color: green;">private readonly ISender _sender;</span><br/>
    <span style="color: green;">public RatingsController(ISender sender)
    {
        _sender = sender;
    }</span>
    ...
    [HttpPost(Name = "CreateArticleRating")]<span style="color: green;">
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]</span>
    public <span style="color: green;">Task<ActionResult<SingleArticleResponse>></span> Create(<span style="color: green;">string slug, [FromBody] NewRatingRequest request, CancellationToken cancellationToken</span>)
    {
        <span style="color: green;">try
        {
            return await _sender.Send(new RateArticleCommand(slug, request.Rating), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new ConflictResult();
        </span>
    }<br/>
...
}<br/>
<span style="color: green;">public record NewRatingRequest(int Rating);</span></pre>
    In <i>Ratings/Commands/Create.cs</i><pre><span style="color: green;">
using Conduit.Application.Extensions;
using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Interfaces;<br/>
using FluentValidation;<br/>
using MediatR;<br/>
namespace Conduit.Application.Features.Ratings.Commands;<br/>
public class RatingCreateValidator : AbstractValidator<RateArticleCommand>
{
    public RatingCreateValidator()
    {
        RuleFor(x => x.Rating).NotNull().NotEmpty().InclusiveBetween(1, 5);
    }
}<br/>
public record RateArticleCommand(string Slug, int Rating) : IRequest<SingleArticleResponse>;<br/>
public class ArticleRateHandler : IRequestHandler<RateArticleCommand, SingleArticleResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;<br/>
    public ArticleRateHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }<br/>
    public async Task<SingleArticleResponse> Handle(RateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles
            .FindAsync(x => x.Slug == request.Slug, cancellationToken);<br/>
        if (article.UserRatings.Any(a => a.UserId == _currentUser.User!.Id))
            throw new InvalidOperationException("You have already rated this article.");<br/>
        article.AddRating(_currentUser.User!, request.Rating);<br/>
        await _context.SaveChangesAsync(cancellationToken);<br/>
        return new SingleArticleResponse(article.Map(_currentUser.User));
    }
}</span></pre>

</details>

### List Ratings for Article 
So we are able to create new ratings, but how can we retrieve them? Moreover, for each article, how can we see all the ratings it received? The answer: a **list** method. 

Modify the list endpoint so that it returns all the ratings for a given article. You will have to create a new query for this. 


<details closed>
    <summary><b>Solution</b></summary>
    In <i>RatingsController.cs</i>
    <pre>...
using Conduit.Application.Features.Ratings.Commands;
<span style="color: green;">using Conduit.Application.Features.Ratings.Queries;
using Conduit.Domain.Entities;</span>
using MediatR;
...
public class RatingsController
{
    ...<br/>
    [HttpGet(Name = "ListArticleRatings")]
    <span style="color: green;">[AllowAnonymous]</span>
    public <span style="color: green;">Task<IReadonlyCollection<ArticleRating>></span>List(<span style="color: green;">string slug, CancellationToken cancellationToken</span>)
    {
        <span style="color: green;">return _sender.Send(new RatingsListQuery(slug), cancellationToken);</span>
    }
...
}<br/></pre>
    In <i>Ratings/Queries/List.cs</i>
    <pre><span style="color: green;">using Conduit.Application.Extensions;
using Conduit.Application.Interfaces;
using Conduit.Domain.Entities;<br/>
using MediatR;<br/>
namespace Conduit.Application.Features.Ratings.Queries;<br/>
public record RatingsListQuery(string Slug) : IRequest<IReadOnlyCollection<ArticleRating>>;<br/>
public class RatingsListHandler : IRequestHandler<RatingsListQuery, IReadOnlyCollection<ArticleRating>>
{
    private readonly IAppDbContext _context;<br/>
    public RatingsListHandler(IAppDbContext context)
    {
        _context = context;
    }<br/>
    public async Task<IReadOnlyCollection<ArticleRating>> Handle(RatingsListQuery request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
        var ratings = article.UserRatings;
        return ratings;
    }
}</span></pre>
</details>

### Delete Rating
Opinions can change over time, and perhaps an article you rated a 5 one year ago feels more like a 3 today. Thus, we want a way to delete ratings as well. Expand the previously defined delete endpoint to support this. 


<details closed>
    <summary><b>Solution</b></summary>
    In <i>Ratings/Commands/Delete.cs</i>
    <pre><span style="color: green;">
using Conduit.Application.Exceptions;
using Conduit.Application.Extensions;
using Conduit.Application.Interfaces;<br/>
using MediatR;<br/>
namespace Conduit.Application.Features.Ratings.Commands;<br/>
public record RatingDeleteCommand(string Slug) : IRequest;<br/>
public class RatingDeleteHandler : IRequestHandler<RatingDeleteCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;<br/>
    public RatingDeleteHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }<br/>
    public async Task Handle(RatingDeleteCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
        var rating = article.UserRatings.FirstOrDefault(x => x.UserId == _currentUser.User!.Id);<br/>
        if (rating == null)
            throw new NotFoundException(nameof(rating));<br/>
        article.RemoveRating(_currentUser.User!);<br/>
        await _context.SaveChangesAsync(cancellationToken);
    }
}</span></pre>
In <i>RatingsController.cs</i>
<pre>
    ... <br/>
    [HttpDelete(Name = "DeleteArticleRating")]<span style="color: green;">
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]</span>
    public async Task DeleteRating(<span style="color: green;">string slug, CancellationToken cancellationToken </span>)
    {
        <span style="color: green;"/>await _sender.Send(new RatingDeleteCommand(slug), cancellationToken);
        return new NoContentResult();</span>
    }<br/>
    ...
</pre>
</details>


### Data Transfer Objects
To improve the security of our application, and not expose all the fields of the ratings, we should use Data Transfer Objects (DTOs). Define data transfer objects and modify the existing code to support these. 

<details closed>
    <summary><b>Solution</b></summary>
    In <i>Ratings/Queries/List.cs</i>
    <pre>using Conduit.Application.Extensions;<span style="color: green;">
using Conduit.Application.Features.Auth.Queries;
using Conduit.Application.Features.Profiles.Queries;</span>
using Conduit.Application.Interfaces;
using Conduit.Domain.Entities;
...
<span style="color: green;">public class RatingDto
{
    public required int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required ProfileDto Author { get; set; }
}<br/>
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
}<br/>
public record MultipleRatingsResponse(IEnumerable<RatingDto> Ratings);</span><br/>
public record RatingsListQuery(string Slug) : IRequest<<span style="color: green;">MultipleRatingsResponse</span>>;<br/>
public class RatingsListHandler : IRequestHandler<RatingsListQuery, MultipleRatingsResponse>
{
    private readonly IAppDbContext _context;<br/>
    public RatingsListHandler(IAppDbContext context)
    {
        _context = context;
    }<br/>
    public async Task<<span style="color: green;">MultipleRatingsResponse</span>> Handle(RatingsListQuery request, CancellationToken cancellationToken)
    {
        var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
        var ratings = article.UserRatings<span style="color: green;">.Select(x => RatingDtoMapper.Map(x, x.User));
        return new MultipleRatingsResponse(ratings);</span>
    }
}</pre>
    In <i>Ratings/Commands/Create.cs</i>
    <pre>
    ...
    <span style="color: green;">public class NewRatingDto
    {
        public int Value { get; set; }
    }</span>
    ...
    </pre>
    In <i>RatingsController.cs</i>
    <pre>
    ...
    public Task<<span style="color: green;">MultipleRatingsResponse</span>> List(string slug, CancellationToken cancellationToken)
    {
        return _sender.Send(new RatingsListQuery(slug), cancellationToken);
    }
    ...
    public async Task<ActionResult<SingleArticleResponse>> Create(string slug, [FromBody] NewRatingRequest request, CancellationToken cancellationToken)
    {
        ...
            return _sender.Send(new RateArticleCommand(slug, request.Rating<span style="color:green;">.Value</span>), cancellationToken);
        ...
    }
    ...
    public record NewRatingRequest(<span style="color: green;">NewRatingDto</span> Rating);
    </pre>
</details>

## Further Work
Congrats 👏 You have completed the provided tasks! What´s next? Try adding new methods or entities to the API, your imagination is the only limit. 
