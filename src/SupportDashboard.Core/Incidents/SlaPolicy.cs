using SupportDashboard.Core.Models;

namespace SupportDashboard.Core.Incidents;

public sealed record SlaStatus(
    DateTimeOffset ResponseDueUtc,
    bool ResponseBreached,
    DateTimeOffset ResolutionDueUtc,
    bool ResolutionBreached);

/// <summary>
/// Placeholder per-priority SLA targets. Once the ServiceNow sync exists, breach status should come
/// from ServiceNow's task_sla records (has_breached) rather than being computed here.
/// </summary>
public static class SlaPolicy
{
    public static TimeSpan ResponseTarget(IncidentPriority priority) => priority switch
    {
        IncidentPriority.P1 => TimeSpan.FromMinutes(15),
        IncidentPriority.P2 => TimeSpan.FromMinutes(30),
        IncidentPriority.P3 => TimeSpan.FromHours(2),
        IncidentPriority.P4 => TimeSpan.FromHours(4),
        _ => TimeSpan.FromHours(8),
    };

    public static TimeSpan ResolutionTarget(IncidentPriority priority) => priority switch
    {
        IncidentPriority.P1 => TimeSpan.FromHours(4),
        IncidentPriority.P2 => TimeSpan.FromHours(8),
        IncidentPriority.P3 => TimeSpan.FromHours(24),
        IncidentPriority.P4 => TimeSpan.FromDays(3),
        _ => TimeSpan.FromDays(5),
    };

    public static SlaStatus Evaluate(Incident incident, DateTimeOffset now)
    {
        var responseDue = incident.OpenedUtc + ResponseTarget(incident.Priority);
        var resolutionDue = incident.OpenedUtc + ResolutionTarget(incident.Priority);
        return new SlaStatus(
            responseDue,
            (incident.RespondedUtc ?? now) > responseDue,
            resolutionDue,
            (incident.ResolvedUtc ?? now) > resolutionDue);
    }
}
