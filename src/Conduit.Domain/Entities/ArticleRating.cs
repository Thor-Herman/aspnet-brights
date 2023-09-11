namespace Conduit.Domain.Entities;

public class ArticleRating
{
    public int ArticleId { get; set; }
    public virtual required Article Article { get; set; }
    public int UserId { get; set; }
    public virtual required User User { get; set; }
    public int Rating { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}