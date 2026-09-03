using OnlineSupermarket.Domain.Reviews;

namespace OnlineSupermarket.Domain.Tests.Reviews;

public class ReviewTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orderItemId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_WhenRatingOutOfRange_ThrowsArgumentOutOfRangeException(int invalidRating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Review.Create(_userId, _orderItemId, _productId, invalidRating, "Comment"));
    }

    [Fact]
    public void Create_WhenCommentExceeds2000Chars_ThrowsArgumentException()
    {
        var longComment = new string('a', 2001);

        Assert.Throws<ArgumentException>(() =>
            Review.Create(_userId, _orderItemId, _productId, 5, longComment));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenCommentIsNullOrWhitespace_SetsCommentToNull(string? emptyComment)
    {
        var review = Review.Create(_userId, _orderItemId, _productId, 5, emptyComment);

        Assert.Null(review.Comment);
    }

    [Fact]
    public void Create_WhenCommentHasSurroundingWhitespace_TrimsComment()
    {
        var review = Review.Create(_userId, _orderItemId, _productId, 5, "  Hài lòng  ");

        Assert.Equal("Hài lòng", review.Comment);
    }

    [Fact]
    public void Create_WhenValid_SetsPropertiesAndTimestamps()
    {
        var before = DateTime.UtcNow;
        var review = Review.Create(_userId, _orderItemId, _productId, 4, "Tốt");
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(_userId, review.UserId);
        Assert.Equal(_orderItemId, review.OrderItemId);
        Assert.Equal(_productId, review.ProductId);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Tốt", review.Comment);
        Assert.InRange(review.CreatedAtUtc, before, after);
        Assert.Equal(review.CreatedAtUtc, review.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WhenValid_UpdatesRatingAndCommentAndRefreshesUpdatedAt()
    {
        var review = Review.Create(_userId, _orderItemId, _productId, 3, "Bình thường");
        var originalUpdated = review.UpdatedAtUtc;

        review.Update(5, "  Rất tốt sau khi dùng thử  ");

        Assert.Equal(5, review.Rating);
        Assert.Equal("Rất tốt sau khi dùng thử", review.Comment);
        Assert.True(review.UpdatedAtUtc >= originalUpdated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Update_WhenRatingOutOfRange_ThrowsArgumentOutOfRangeException(int invalidRating)
    {
        var review = Review.Create(_userId, _orderItemId, _productId, 5, "Tốt");

        Assert.Throws<ArgumentOutOfRangeException>(() => review.Update(invalidRating, "Comment"));
    }

    [Fact]
    public void Update_WhenCommentExceeds2000Chars_ThrowsArgumentException()
    {
        var review = Review.Create(_userId, _orderItemId, _productId, 5, "Tốt");
        var longComment = new string('x', 2001);

        Assert.Throws<ArgumentException>(() => review.Update(5, longComment));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Update_WhenCommentEmpty_NormalizesToNull(string? emptyComment)
    {
        var review = Review.Create(_userId, _orderItemId, _productId, 5, "Tốt ban đầu");

        review.Update(4, emptyComment);

        Assert.Null(review.Comment);
    }

    [Fact]
    public void Create_WhenRequiredIdsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Review.Create(Guid.Empty, _orderItemId, _productId, 5, "Tốt"));
        Assert.Throws<ArgumentException>(() => Review.Create(_userId, Guid.Empty, _productId, 5, "Tốt"));
        Assert.Throws<ArgumentException>(() => Review.Create(_userId, _orderItemId, Guid.Empty, 5, "Tốt"));
    }
}
