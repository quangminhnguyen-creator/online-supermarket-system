using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Jobs;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.Jobs;

public class JobRunCoordinator(AppDbContext dbContext, IJobQueue jobQueue)
{
    public async Task<bool> TryQueueAsync(string jobName, string lockKey, CancellationToken cancellationToken)
    {
        // For testing with InMemory provider which doesn't enforce unique constraints
        var exists = await dbContext.BackgroundJobRuns.AnyAsync(x => x.JobName == jobName && x.LockKey == lockKey, cancellationToken);
        if (exists) return false;

        var run = new BackgroundJobRun(jobName, lockKey, DateTime.UtcNow);
        dbContext.BackgroundJobRuns.Add(run);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return false;
        }

        try
        {
            await jobQueue.EnqueueAsync(new JobRequest(run.Id, jobName), cancellationToken);
        }
        catch
        {
            // Ignore channel errors to ensure durable DB persistence (job will be picked up by recovery)
        }

        return true;
    }
}
