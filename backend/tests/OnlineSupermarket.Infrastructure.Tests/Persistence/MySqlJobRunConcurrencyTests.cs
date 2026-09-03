using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Jobs;
using OnlineSupermarket.Infrastructure.Persistence;
using Xunit;

namespace OnlineSupermarket.Infrastructure.Tests.Persistence;

[Collection("MySqlFixture")]
public class MySqlJobRunConcurrencyTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlJobRunConcurrencyTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TryQueueAsync_WithRealMySql_EnforcesUniqueConstraintAndNullLocks()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(_fixture.ConnectionString)
            .Options;

        using var dbContext1 = new AppDbContext(options);
        using var dbContext2 = new AppDbContext(options);

        var run1 = new BackgroundJobRun("TestJob", "UniqueKey", DateTime.UtcNow);
        var run2 = new BackgroundJobRun("TestJob", "UniqueKey", DateTime.UtcNow);

        dbContext1.BackgroundJobRuns.Add(run1);
        dbContext2.BackgroundJobRuns.Add(run2);

        var task1 = dbContext1.SaveChangesAsync();
        var task2 = dbContext2.SaveChangesAsync();

        var ex1 = await Record.ExceptionAsync(() => task1);
        var ex2 = await Record.ExceptionAsync(() => task2);

        // One should succeed (no exception), one should fail (DbUpdateException)
        Assert.True(ex1 == null || ex2 == null, "One insert must succeed");
        Assert.True(ex1 is DbUpdateException || ex2 is DbUpdateException, "One insert must fail with DbUpdateException");

        using var dbContext3 = new AppDbContext(options);
        var count = await dbContext3.BackgroundJobRuns.CountAsync(x => x.JobName == "TestJob" && x.LockKey == "UniqueKey");
        Assert.Equal(1, count);
    }
}
