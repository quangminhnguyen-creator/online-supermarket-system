namespace OnlineSupermarket.Infrastructure.Jobs;

public record JobRequest(Guid RunId, string JobName);

public interface IJobQueue
{
    ValueTask EnqueueAsync(JobRequest request, CancellationToken cancellationToken);
    ValueTask<JobRequest> DequeueAsync(CancellationToken cancellationToken);
}
