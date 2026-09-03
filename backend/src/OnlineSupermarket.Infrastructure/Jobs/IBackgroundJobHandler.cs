namespace OnlineSupermarket.Infrastructure.Jobs;

public interface IBackgroundJobHandler
{
    string JobName { get; }
    Task HandleAsync(Guid runId, CancellationToken cancellationToken);
}
