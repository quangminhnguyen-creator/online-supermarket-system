namespace OnlineSupermarket.Domain.Jobs;

public enum JobRunStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}
