namespace POSCartService.Services;

public sealed class OutboxPublisherOptions
{
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 1;
    public int LeaseDurationSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 10;
    public int BaseRetryDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 300;
}
