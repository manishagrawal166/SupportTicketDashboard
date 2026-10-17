namespace SupportDashboard.Core.Models;

public enum RunStatus
{
    Running,
    Succeeded,
    Failed,
}

public sealed class FunctionExecution
{
    public required string FunctionName { get; init; }
    public required string AppName { get; init; }
    public required RunStatus Status { get; init; }
    public required string Trigger { get; init; }
    public required DateTimeOffset StartUtc { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class PipelineRun
{
    public required string PipelineName { get; init; }
    public required string RunId { get; init; }
    public required RunStatus Status { get; init; }
    public required string TriggerType { get; init; }
    public required DateTimeOffset StartUtc { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class WorkflowRun
{
    public required string WorkflowName { get; init; }
    public required RunStatus Status { get; init; }
    public required string Trigger { get; init; }
    public required DateTimeOffset StartUtc { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? FailedAction { get; init; }
}
