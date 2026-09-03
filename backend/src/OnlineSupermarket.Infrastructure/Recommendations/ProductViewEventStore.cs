using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.Recommendations;

public sealed class ProductViewEventStore(AppDbContext dbContext) : IProductViewEventStore
{
    public async Task<int> MergeAnonymousSessionAsync(
        Guid anonymousSessionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProductViewEvents
            .Where(view => view.AnonymousSessionId == anonymousSessionId && view.UserId == null)
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(view => view.UserId, userId),
                cancellationToken);
    }
}