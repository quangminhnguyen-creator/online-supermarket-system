using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Review;

public sealed record CreateReviewRequest(
    [property: JsonPropertyName("orderItemId")] Guid OrderItemId,
    [property: JsonPropertyName("rating")] int Rating,
    [property: JsonPropertyName("comment")] string? Comment);

public sealed record UpdateReviewRequest(
    [property: JsonPropertyName("rating")] int Rating,
    [property: JsonPropertyName("comment")] string? Comment);

public sealed record ReviewDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("productId")] Guid ProductId,
    [property: JsonPropertyName("reviewerName")] string ReviewerName,
    [property: JsonPropertyName("rating")] int Rating,
    [property: JsonPropertyName("comment")] string? Comment,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTime UpdatedAtUtc);

public sealed record ProductReviewsDto(
    [property: JsonPropertyName("averageRating")] decimal AverageRating,
    [property: JsonPropertyName("reviewCount")] int ReviewCount,
    [property: JsonPropertyName("data")] IReadOnlyList<ReviewDto> Data,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalCount")] int TotalCount);

public sealed record ReviewEligibilityDto(
    [property: JsonPropertyName("canReview")] bool CanReview,
    [property: JsonPropertyName("orderItemId")] Guid? OrderItemId,
    [property: JsonPropertyName("reviewId")] Guid? ReviewId,
    [property: JsonPropertyName("existingRating")] int? ExistingRating = null,
    [property: JsonPropertyName("existingComment")] string? ExistingComment = null);
