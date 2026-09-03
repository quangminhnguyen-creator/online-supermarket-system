using System.Threading.Channels;

namespace OnlineSupermarket.Infrastructure.Jobs;

public class ChannelJobQueue : IJobQueue
{
    private readonly Channel<JobRequest> _channel;

    public ChannelJobQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<JobRequest>(options);
    }

    public ValueTask EnqueueAsync(JobRequest request, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public ValueTask<JobRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
