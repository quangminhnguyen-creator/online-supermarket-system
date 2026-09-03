namespace OnlineSupermarket.Infrastructure.Recommendations;

public interface IProductViewEventStore
{
    Task<int> MergeAnonymousSessionAsync(
        Guid anonymousSessionId,
        Guid userId,
        CancellationToken cancellationToken);
}