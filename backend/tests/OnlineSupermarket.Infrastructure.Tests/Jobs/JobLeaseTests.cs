using Microsoft.EntityFrameworkCore;
using Moq;
using OnlineSupermarket.Domain.Jobs;
using OnlineSupermarket.Infrastructure.Jobs;
using OnlineSupermarket.Infrastructure.Persistence;
using Xunit;

namespace OnlineSupermarket.Infrastructure.Tests.Jobs;

public class JobLeaseTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IJobQueue> _queueMock;
    private readonly JobLeaseService _sut;

    public JobLeaseTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        _dbContext = new AppDbContext(options);
        _queueMock = new Mock<IJobQueue>();
        _sut = new JobLeaseService(_dbContext, _queueMock.Object, TimeProvider.System);
    }

    [Fact]
    public async Task RenewLease_WhenValid_ShouldUpdateLease()
    {
        var run = new BackgroundJobRun("TestJob", "Key1", DateTime.UtcNow);
        run.Start("token1", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        _dbContext.BackgroundJobRuns.Add(run);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.TryRenewLeaseAsync(run.Id, "token1", TimeSpan.FromMinutes(5), CancellationToken.None);
        
        Assert.True(result);
        var dbRow = await _dbContext.BackgroundJobRuns.SingleAsync(x => x.Id == run.Id);
        Assert.True(dbRow.LeaseExpiresAtUtc > DateTime.UtcNow.AddMinutes(4));
    }

    [Fact]
    public async Task RecoverStartupJobs_ShouldRequeueStaleAndQueuedJobs()
    {
        var queuedRun = new BackgroundJobRun("TestJob1", "Key1", DateTime.UtcNow.AddHours(-1));
        
        var staleRun = new BackgroundJobRun("TestJob2", "Key2", DateTime.UtcNow.AddHours(-1));
        staleRun.Start("token1", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(-30));
        
        _dbContext.BackgroundJobRuns.AddRange(queuedRun, staleRun);
        await _dbContext.SaveChangesAsync();

        await _sut.RecoverStaleJobsAsync(CancellationToken.None);

        var staleDb = await _dbContext.BackgroundJobRuns.SingleAsync(x => x.Id == staleRun.Id);
        Assert.Equal(JobRunStatus.Failed, staleDb.Status); // Fail the stale run automatically

        // Requeue queued runs
        _queueMock.Verify(q => q.EnqueueAsync(It.Is<JobRequest>(r => r.RunId == queuedRun.Id), It.IsAny<CancellationToken>()), Times.Once);
    }
}
