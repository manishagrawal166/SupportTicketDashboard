namespace SupportDashboard.Core.Models;

public enum IncidentPriority
{
    P1 = 1,
    P2 = 2,
    P3 = 3,
    P4 = 4,
    P5 = 5,
}

public static class IncidentStates
{
    public const string Open = "Open";
    public const string InProgress = "In Progress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Canceled = "Canceled";

    public static bool IsActive(string state) =>
        state is not (Resolved or Closed or Canceled);
}

public sealed class Incident
{
    public required string Number { get; init; }
    public required string ShortDescription { get; init; }
    public required IncidentPriority Priority { get; init; }
    public required string State { get; set; }
    public required string AssignmentGroup { get; init; }
    public required string AssignedBy { get; init; }
    public required string AssignedTo { get; init; }
    public required DateTimeOffset OpenedUtc { get; init; }
    public DateTimeOffset? RespondedUtc { get; set; }
    public DateTimeOffset? ResolvedUtc { get; set; }
    public string? Comment { get; set; }
}
