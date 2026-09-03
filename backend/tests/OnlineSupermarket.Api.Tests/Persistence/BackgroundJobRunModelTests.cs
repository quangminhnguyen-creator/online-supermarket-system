using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Infrastructure.Persistence;
using Xunit;

namespace OnlineSupermarket.Api.Tests.Persistence;

public class BackgroundJobRunModelTests
{
    [Fact]
    public void BackgroundJobRun_ShouldBeConfiguredCorrectly()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_BackgroundJobRun_Model")
            .Options;
            
        using var context = new AppDbContext(options);
        var model = context.Model;
        
        var entityType = model.FindEntityType("OnlineSupermarket.Domain.Jobs.BackgroundJobRun");
        Assert.NotNull(entityType);
        
        Assert.Equal("background_job_runs", entityType.GetTableName());
        
        var idProp = entityType.FindProperty("Id");
        Assert.NotNull(idProp);
        Assert.True(idProp.IsPrimaryKey());
        
        var jobNameProp = entityType.FindProperty("JobName");
        Assert.NotNull(jobNameProp);
        Assert.False(jobNameProp.IsNullable);
        Assert.Equal(100, jobNameProp.GetMaxLength());
        
        var lockKeyProp = entityType.FindProperty("LockKey");
        Assert.NotNull(lockKeyProp);
        Assert.False(lockKeyProp.IsNullable);
        Assert.Equal(100, lockKeyProp.GetMaxLength());
        
        var statusProp = entityType.FindProperty("Status");
        Assert.NotNull(statusProp);
        Assert.False(statusProp.IsNullable);
        Assert.Equal(20, statusProp.GetMaxLength());
        
        var lockTokenProp = entityType.FindProperty("LockToken");
        Assert.NotNull(lockTokenProp);
        Assert.True(lockTokenProp.IsNullable);
        Assert.Equal(50, lockTokenProp.GetMaxLength());
        
        var errorSummaryProp = entityType.FindProperty("ErrorSummary");
        Assert.NotNull(errorSummaryProp);
        Assert.True(errorSummaryProp.IsNullable);
        Assert.Equal(1000, errorSummaryProp.GetMaxLength());
        
        // Unique index check
        var index = entityType.GetIndexes().FirstOrDefault(i => i.IsUnique && i.Properties.Count == 2 
            && i.Properties.Any(p => p.Name == "JobName") 
            && i.Properties.Any(p => p.Name == "LockKey"));
            
        Assert.NotNull(index);
    }
}
