using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OnlineSupermarket.Infrastructure.Jobs;

public class IntelligenceWorker(
    IJobQueue jobQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<IntelligenceWorker> logger) : BackgroundService
{
    private const int MaxConcurrentJobs = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var semaphore = new SemaphoreSlim(MaxConcurrentJobs);
        var tasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await jobQueue.DequeueAsync(stoppingToken);

                await semaphore.WaitAsync(stoppingToken);

                // Start handling the job in background
                var task = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        var handlers = scope.ServiceProvider.GetServices<IBackgroundJobHandler>();
                        var handler = handlers.FirstOrDefault(h => h.JobName == request.JobName);

                        if (handler == null)
                        {
                            logger.LogWarning("No handler found for job name {JobName}", request.JobName);
                            return;
                        }

                        await handler.HandleAsync(request.RunId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing job {JobName} ({RunId})", request.JobName, request.RunId);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, stoppingToken);

                tasks.Add(task);
                tasks.RemoveAll(t => t.IsCompleted); // cleanup completed tasks
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error dequeuing job");
                await Task.Delay(1000, stoppingToken);
            }
        }

        // Wait for all currently running jobs to finish gracefully
        await Task.WhenAll(tasks);
    }
}
