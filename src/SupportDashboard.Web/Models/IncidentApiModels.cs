using System.ComponentModel.DataAnnotations;
using SupportDashboard.Core.Incidents;
using SupportDashboard.Core.Models;
using SupportDashboard.Web.Formatting;

namespace SupportDashboard.Web.Models;

public sealed record IncidentDto(
    string Number,
    string ShortDescription,
    string Priority,
    int PriorityRank,
    string State,
    string AssignmentGroup,
    string AssignedBy,
    string AssignedTo,
    DateTimeOffset OpenedUtc,
    string Age,
    bool IsNew,
    DateTimeOffset ResponseDueUtc,
    bool ResponseBreached,
    DateTimeOffset ResolutionDueUtc,
    bool ResolutionBreached,
    string? Comment)
{
    public static IncidentDto From(Incident incident, DateTimeOffset now)
    {
        var sla = SlaPolicy.Evaluate(incident, now);
        return new IncidentDto(
            incident.Number,
            incident.ShortDescription,
            incident.Priority.ToString(),
            (int)incident.Priority,
            incident.State,
            incident.AssignmentGroup,
            incident.AssignedBy,
            incident.AssignedTo,
            incident.OpenedUtc,
            DisplayFormat.Age(incident.OpenedUtc, now),
            DisplayFormat.IsNew(incident.OpenedUtc, now),
            sla.ResponseDueUtc,
            sla.ResponseBreached,
            sla.ResolutionDueUtc,
            sla.ResolutionBreached,
            incident.Comment);
    }
}

public sealed record IncidentListResponse(
    PriorityCounts Totals,
    PriorityCounts Facets,
    IReadOnlyList<IncidentDto> Items,
    DateTimeOffset GeneratedUtc);

public sealed class CommentRequest
{
    [StringLength(2000)]
    public string? Comment { get; init; }
}
