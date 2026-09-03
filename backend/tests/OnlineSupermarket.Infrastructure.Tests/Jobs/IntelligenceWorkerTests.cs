using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OnlineSupermarket.Infrastructure.Jobs;
using Xunit;

namespace OnlineSupermarket.Infrastructure.Tests.Jobs;

public class IntelligenceWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldDispatchJobAndIsolateExceptions()
    {
        var queueMock = new Mock<IJobQueue>();
        var handlerMock = new Mock<IBackgroundJobHandler>();
        var runId1 = Guid.NewGuid();
        var runId2 = Guid.NewGuid();

        handlerMock.SetupGet(h => h.JobName).Returns("KnownJob");
        handlerMock.Setup(h => h.HandleAsync(runId1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failed"));
        handlerMock.Setup(h => h.HandleAsync(runId2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Enqueue 3 items: known (fails), unknown, known (succeeds)
        var sequence = new Queue<JobRequest>(new[]
        {
            new JobRequest(runId1, "KnownJob"),
            new JobRequest(Guid.NewGuid(), "UnknownJob"),
            new JobRequest(runId2, "KnownJob")
        });

        queueMock.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns(() => 
            {
                if (sequence.Count > 0)
                    return new ValueTask<JobRequest>(sequence.Dequeue());
                    
                // Keep waiting indefinitely after queue is empty to simulate blocking wait
                return new ValueTask<JobRequest>(Task.Delay(Timeout.InfiniteTimeSpan).ContinueWith(_ => (JobRequest)null!));
            });

        var worker = new IntelligenceWorker(
            queueMock.Object, 
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<IntelligenceWorker>.Instance);

        var cts = new CancellationTokenSource();
        var executeTask = worker.StartAsync(cts.Token);
        
        // Let it process
        await Task.Delay(100);
        await cts.CancelAsync();
        
        handlerMock.Verify(h => h.HandleAsync(runId1, It.IsAny<CancellationToken>()), Times.Once);
        handlerMock.Verify(h => h.HandleAsync(runId2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
