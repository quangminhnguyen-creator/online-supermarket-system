using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Reviews;

public sealed class Review : Entity
{
    private Review()
    {
    }

    public Guid UserId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Review Create(
        Guid userId,
        Guid orderItemId,
        Guid productId,
        int rating,
        string? comment)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (orderItemId == Guid.Empty)
        {
            throw new ArgumentException("Order item ID is required.", nameof(orderItemId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        ValidateRating(rating);
        var trimmedComment = NormalizeComment(comment);

        var now = DateTime.UtcNow;

        return new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderItemId = orderItemId,
            ProductId = productId,
            Rating = rating,
            Comment = trimmedComment,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(int rating, string? comment)
    {
        ValidateRating(rating);
        var trimmedComment = NormalizeComment(comment);

        Rating = rating;
        Comment = trimmedComment;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        }
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var trimmed = comment.Trim();
        if (trimmed.Length > 2000)
        {
            throw new ArgumentException("Comment must not exceed 2000 characters.", nameof(comment));
        }

        return trimmed;
    }
}
