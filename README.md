# Ratings System

This repo is a fork of [Realworld app](https://github.com/adr1enbe4udou1n/aspnetcore-realworld-example-app) intended for a brights course on dotnet.

## Table of Contents

- [Project Setup](#project-setup)
  - [Prerequisities](#prerequisites)
  - [Start the app](#start-the-app)
- [Data Model](#data-model)
  - [Rating Model](#rating-model)
  - [Relations](#relations)
  - [Migrations](#migrations)
- [Controller](#controller)
  - [End Points](#end-points)
  - [Create Rating](#create-ratings)
  - [List Ratings](#list-ratings)
  - [Delete Rating](#delete-ratings)
  - [Data Transfer Objects](#data-transfer-objects)
- [Further Work](#further-work)

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
    The solution (<b>Realworld.sln</b>) is divided into several smaller projects that handle different responsibilities
    <ul>
      <li><i>Conduit.Application</i> contains the commands and queries that define the functionality. All interfaces can be found here too.</li>
      <li><i>Conduit.Domain</i> has the definitions for the database entities</li>
      <li><i>Conduit.Presentation</i> defines the end points and controllers that handle incoming requests. It also sets up Swagger in the ServiceExtension</li>
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

```csharp
namespace Conduit.Domain.Entities;

public class ArticleRating
{
    public int ArticleId { get; set; }
    public virtual required Article Article { get; set; }
    public int UserId { get; set; }
    public virtual required User User { get; set; }
    public int Rating { get; set; }
}
```

</details>

### Relations

With the class and its properties defined, its relations to other entites must also be declared. Set up the necessary relations in the _DbContext_.

<details closed>
    <summary><b>Solution</b></summary>
    Here is a possible solution that implements the relation presented previously. 
    
In _AppDbContext.cs_:

```diff
    ...

+    modelBuilder.Entity<ArticleRating>(b =>
+    {
+        b.HasKey(e => new { e.ArticleId, e.UserId });
+        b.HasOne(e => e.Article)
+            .WithMany(e => e.UserRatings)
+            .HasForeignKey(e => e.ArticleId);
+        b.HasOne(e => e.User)
+            .WithMany(e => e.ArticleRatings)
+            .HasForeignKey(e => e.UserId);
+    });</span>
    ...
```

In _User.cs_

```diff
    ...
    private readonly List<ArticleFavorite> _favoriteArticles = new();
+   private readonly List<ArticleRating> _articleRatings = new();
    ...
    public virtual IReadOnlyCollection<ArticleFavorite> FavoriteArticles => _favoriteArticles;
+   public virtual IReadOnlyCollection<ArticleRating> ArticleRatings => _articleRatings;
```

In _Article.cs_

```diff
    ...
    private readonly List<ArticleTag> _tags = new();
    private readonly List<ArticleFavorite> _favoredUsers = new();
+   private readonly List<ArticleRating> _userRatings = new();
    ...
    public virtual IReadOnlyCollection<ArticleFavorite> FavoredUsers => _favoredUsers;
+   public virtual IReadOnlyCollection<ArticleRating> UserRatings => _userRatings;
    ...
+   public void RemoveRating(User user) => _userRatings.RemoveAll(x => x.UserId == user.Id);
+   public void AddRating(User user, int rating) => _userRatings.Add(new ArticleRating { User = user, Article = this, Rating = rating });
```

</details>

### Migrations

To apply the changes in the data model to the PostgreSQL database, you will have to create a migration. You can use the `dotnet ef` tool for this. To verify that the migration worked as intended, you can use a tool like pgadmin or the postgres CLI to inspect the database.

<details closed>
    <summary><b>Solution</b></summary>
    Perform the following commands in the _Conduit.Infrastructure_ folder:
    
    $ dotnet clean
    $ dotnet build
    $ dotnet ef migrations add "<Name of your migration>" -s ../Conduit.WebUI
    $ dotnet ef database update -s ../Conduit.WebUI
</details>
<br/>
    
## Controller
Now that we have updated our data model, it´s time to move on to the controller responsible for handling rating requests. Start by creating a new controller in the correct folder.

### End Points

Before we start implementing the desired rating functionality, we should ensure that our desired endpoints are reachable. Thus, you should start by creating methods handling the http calls. You can start with returning 200 response codes.

The controller should support the following methods:

- POST
- GET
- DELETE

At the url `http://localhost:5000/api/articles/{article_slug}/ratings`

<details closed>
    <summary><b>Solution</b></summary>

_In RatingsController.cs_
    
```csharp
using Conduit.Application.Features.Ratings.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Presentation.Controllers;

[Route("articles/{slug}/[controller]")]
[ApiExplorerSettings(GroupName = "Ratings")]
[Authorize]
public class RatingsController
{

    [HttpGet(Name = "ListArticleRatings")]
    public IActionResult List()
    {
        return new OkObjectResult("List");
    }

    [HttpPost(Name = "CreateArticleRating")]
    public IActionResult Create()
    {
        return new OkObjectResult("Create");
    }

    [HttpDelete(Name = "DeleteArticleRating")]
    public IActionResult DeleteRating()
    {
        return new OkObjectResult("Delete");
    }

}

```
</details>

### Create Rating

Now that we have a skeleton for the incoming requests, it´s time to implement the actual logic. The first step is to define the create method. Create the necessary commands and update the controller with new logic to support the creation of new ratings.

> **N.B.** Remember to validate the rating! It should only allow integers between 1 and 5.

<details closed>
    <summary><b>Solution</b></summary>

In _RatingsController.cs_

```diff
+using Conduit.Application.Features.Articles.Queries;
+using Conduit.Application.Features.Ratings.Commands;
+using MediatR;
 using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Mvc;
+using Microsoft.AspNetCore.Http;
...
public class RatingsController
{
+    private readonly ISender _sender;
+    public RatingsController(ISender sender)
+    {
+        _sender = sender;
+    }
     ...
     [HttpPost(Name = "CreateArticleRating")]
+    [ProducesResponseType(StatusCodes.Status200OK)]
+    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
+    [ProducesResponseType(StatusCodes.Status404NotFound)]
+    [ProducesResponseType(StatusCodes.Status409Conflict)]
+    public Task<ActionResult<SingleArticleResponse>> Create(string slug, [FromBody] NewRatingRequest request, CancellationToken cancellationToken)
    {
+        try
+        {
+            return await _sender.Send(new RateArticleCommand(slug, request.Rating), cancellationToken);
+        }
+        catch (InvalidOperationException)
+        {
+            return new ConflictResult();
+        }
+    }
...
}
+public record NewRatingRequest(int Rating);
```

In _Ratings/Commands/Create.cs_

```csharp
using Conduit.Application.Extensions;
using Conduit.Application.Features.Articles.Queries;
using Conduit.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Conduit.Application.Features.Ratings.Commands
{
    public class RatingCreateValidator : AbstractValidator<RateArticleCommand>
    {
        public RatingCreateValidator()
        {
            RuleFor(x => x.Rating).NotNull().NotEmpty().InclusiveBetween(1, 5);
        }
    }

    public record RateArticleCommand(string Slug, int Rating) : IRequest<SingleArticleResponse>;

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

            article.AddRating(_currentUser.User!, request.Rating);
            await _context.SaveChangesAsync(cancellationToken);
            return new SingleArticleResponse(article.Map(_currentUser.User));
        }
    }
}
```

</details>

### List Ratings for Article

So we are able to create new ratings, but how can we retrieve them? Moreover, for each article, how can we see all the ratings it received? The answer: a **list** method.

Modify the list endpoint so that it returns all the ratings for a given article. You will have to create a new query for this.

<details closed>
    <summary><b>Solution</b></summary>

In _RatingsController.cs_
    
```diff
...
using Conduit.Application.Features.Ratings.Commands;
+using Conduit.Application.Features.Ratings.Queries;
+using Conduit.Domain.Entities;
using MediatR;
...

public class RatingsController
{
...
  [HttpGet(Name = "ListArticleRatings")]
+ [AllowAnonymous]
+ public Task<IReadonlyCollection<ArticleRating>> List(string slug, CancellationToken cancellationToken)
  {
+        return _sender.Send(new RatingsListQuery(slug), cancellationToken);
  }
  ...
}
```

In _Ratings/Queries/List.cs_

```csharp
using Conduit.Application.Extensions;
using Conduit.Application.Interfaces;
using Conduit.Domain.Entities;
using MediatR;

namespace Conduit.Application.Features.Ratings.Queries
{
    public record RatingsListQuery(string Slug) : IRequest<IReadOnlyCollection<ArticleRating>>;

    public class RatingsListHandler : IRequestHandler<RatingsListQuery, IReadOnlyCollection<ArticleRating>>
    {
        private readonly IAppDbContext _context;

        public RatingsListHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<ArticleRating>> Handle(RatingsListQuery request, CancellationToken cancellationToken)
        {
            var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
            var ratings = article.UserRatings;
            return ratings;
        }
    }
}
```

</details>

### Delete Rating

Opinions can change over time, and perhaps an article you rated a 5 one year ago feels more like a 3 today. Thus, we want a way to delete ratings as well. Expand the previously defined delete endpoint to support this.

<details closed>
    <summary><b>Solution</b></summary>

In _Ratings/Commands/Delete.cs_

```csharp
using Conduit.Application.Exceptions;
using Conduit.Application.Extensions;
using Conduit.Application.Interfaces;
using MediatR;

namespace Conduit.Application.Features.Ratings.Commands
{
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

            article.RemoveRating(_currentUser.User!);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

In _RatingsController.cs_

```diff
     ...
     [HttpDelete(Name = "DeleteArticleRating")]
+    [ProducesResponseType(StatusCodes.Status204NoContent)]
+    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
+    [ProducesResponseType(StatusCodes.Status404NotFound)]
     public async Task DeleteRating(string slug, CancellationToken cancellationToken)
     {
        +await _sender.Send(new RatingDeleteCommand(slug), cancellationToken);
        +return new NoContentResult();
     }
     ...
```

</details>

### Data Transfer Objects

To improve the security of our application, and not expose all the fields of the ratings, we should use Data Transfer Objects (DTOs). Define data transfer objects and modify the existing code to support these.

<details closed>
    <summary><b>Solution</b></summary>

In _Ratings/Queries/List.cs_
    
```diff
using Conduit.Application.Extensions;
+using Conduit.Application.Features.Auth.Queries;
+using Conduit.Application.Features.Profiles.Queries;
using Conduit.Application.Interfaces;
using Conduit.Domain.Entities;
...

+public class RatingDto
+{

+ public required int Value { get; set; }
+ public DateTime CreatedAt { get; set; }
+ public DateTime UpdatedAt { get; set; }
+ public required ProfileDto Author { get; set; }
  +}

+public static class RatingDtoMapper
+{

+ public static RatingDto Map(ArticleRating rating, User? user)
+ {
+        return new()
+        {
+            Value = rating.Rating,
+            CreatedAt = rating.CreatedAt,
+            UpdatedAt = rating.UpdatedAt,
+            Author = rating.User.MapToProfile(user),
+        };
+ }
  +}

+public record MultipleRatingsResponse(IEnumerable<RatingDto> Ratings);

+public record RatingsListQuery(string Slug) : IRequest<MultipleRatingsResponse>;

+public class RatingsListHandler : IRequestHandler<RatingsListQuery, MultipleRatingsResponse>
+{

  private readonly IAppDbContext _context;
 
  public RatingsListHandler(IAppDbContext context)
  {
         _context = context;
  }
 
+ public async Task<MultipleRatingsResponse> Handle(RatingsListQuery request, CancellationToken cancellationToken)
  {
         var article = await _context.Articles.FindAsync(x => x.Slug == request.Slug, cancellationToken);
+         var ratings = article.UserRatings.Select(x => RatingDtoMapper.Map(x, x.User));
+         return new MultipleRatingsResponse(ratings);
  }
  +}

```

In _Ratings/Commands/Create.cs_

```diff
    ...
+   public class NewRatingDto
+   {
+        public int Value { get; set; }
+   }
    ...
```

In _RatingsController.cs_

```diff
    ...
+   public Task<MultipleRatingsResponse> List(string slug, CancellationToken cancellationToken)
    {
        return _sender.Send(new RatingsListQuery(slug), cancellationToken);
    }
    ...
+   public async Task<ActionResult<SingleArticleResponse>> Create(string slug, [FromBody] NewRatingRequest request, CancellationToken cancellationToken)
    {
        ...
+           return _sender.Send(new RateArticleCommand(slug, request.Rating.Value), cancellationToken);
        ...
    }
    ...
+   public record NewRatingRequest(NewRatingDto Rating);
```

</details>

## Further Work

Congrats 👏 You have completed the provided tasks! What´s next? Try adding new methods or entities to the API, your imagination is the only limit.
