using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Jobs;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.Jobs;

public class JobLeaseService(
    AppDbContext dbContext,
    IJobQueue jobQueue,
    TimeProvider timeProvider)
{
    public async Task<bool> TryRenewLeaseAsync(Guid runId, string token, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        var run = await dbContext.BackgroundJobRuns.FirstOrDefaultAsync(x => x.Id == runId && x.LockToken == token, cancellationToken);
        if (run == null || run.Status != JobRunStatus.Running) return false;

        run.RenewLease(token, timeProvider.GetUtcNow().UtcDateTime.Add(leaseDuration));
        
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task RecoverStaleJobsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // 1. Requeue pending items that were queued but never started (e.g. queue was lost during restart)
        var queuedRuns = await dbContext.BackgroundJobRuns
            .Where(r => r.Status == JobRunStatus.Queued)
            .ToListAsync(cancellationToken);

        foreach (var run in queuedRuns)
        {
            try
            {
                await jobQueue.EnqueueAsync(new JobRequest(run.Id, run.JobName), cancellationToken);
            }
            catch
            {
                // ignore
            }
        }

        // 2. Fail stale running jobs (lease expired)
        var staleRuns = await dbContext.BackgroundJobRuns
            .Where(r => r.Status == JobRunStatus.Running && r.LeaseExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        foreach (var run in staleRuns)
        {
            run.MarkAsFailed(run.LockToken!, now, "Lease expired and job abandoned");
        }

        if (staleRuns.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
