using SupportDashboard.Core.Models;
using SupportDashboard.Core.Time;

namespace SupportDashboard.Core.Incidents;

public enum IncidentSortField
{
    Number,
    Description,
    Priority,
    State,
    Group,
    AssignedBy,
    AssignedTo,
    Opened,
    Age,
}

public enum SortDirection
{
    Asc,
    Desc,
}

/// <summary>Breach filters only match active (unresolved) incidents, matching the Overview SLA cards.</summary>
public enum SlaFilter
{
    None,
    ResponseBreached,
    ResolutionBreached,
}

public sealed record IncidentQuery(
    IncidentPriority? Priority = null,
    string? Search = null,
    DateOnly? From = null,
    DateOnly? To = null,
    IncidentSortField Sort = IncidentSortField.Priority,
    SortDirection Direction = SortDirection.Asc,
    SlaFilter Sla = SlaFilter.None);

public sealed record PriorityCounts(int Total, int P1, int P2, int P3, int P4, int P5)
{
    public static PriorityCounts From(IEnumerable<Incident> incidents)
    {
        int total = 0, p1 = 0, p2 = 0, p3 = 0, p4 = 0, p5 = 0;
        foreach (var incident in incidents)
        {
            total++;
            switch (incident.Priority)
            {
                case IncidentPriority.P1: p1++; break;
                case IncidentPriority.P2: p2++; break;
                case IncidentPriority.P3: p3++; break;
                case IncidentPriority.P4: p4++; break;
                case IncidentPriority.P5: p5++; break;
            }
        }
        return new PriorityCounts(total, p1, p2, p3, p4, p5);
    }
}

/// <param name="Totals">Counts across every incident, unaffected by filters (the stat tiles).</param>
/// <param name="Facets">Counts after search/date filters but before the priority filter (the chip counts).</param>
public sealed record IncidentQueryResult(PriorityCounts Totals, PriorityCounts Facets, IReadOnlyList<Incident> Items);

public static class IncidentQueryEvaluator
{
    public static IncidentQueryResult Evaluate(IEnumerable<Incident> incidents, IncidentQuery query, DateTimeOffset now)
    {
        var all = incidents.ToList();
        var terms = ParseSearchTerms(query.Search);

        IEnumerable<Incident> filtered = all;
        if (terms.Length > 0)
        {
            filtered = filtered.Where(i => terms.Any(t =>
                i.Number.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                i.ShortDescription.Contains(t, StringComparison.OrdinalIgnoreCase)));
        }
        if (query.From is { } from)
        {
            var fromUtc = CentralTime.StartOfDayUtc(from);
            filtered = filtered.Where(i => i.OpenedUtc >= fromUtc);
        }
        if (query.To is { } to)
        {
            var toExclusiveUtc = CentralTime.StartOfDayUtc(to.AddDays(1));
            filtered = filtered.Where(i => i.OpenedUtc < toExclusiveUtc);
        }
        if (query.Sla != SlaFilter.None)
        {
            filtered = filtered.Where(i => IsBreached(i, query.Sla, now));
        }

        var faceted = filtered.ToList();
        IEnumerable<Incident> rows = faceted;
        if (query.Priority is { } priority)
        {
            rows = rows.Where(i => i.Priority == priority);
        }

        return new IncidentQueryResult(
            PriorityCounts.From(all),
            PriorityCounts.From(faceted),
            Sort(rows, query.Sort, query.Direction).ToList());
    }

    public static bool IsBreached(Incident incident, SlaFilter sla, DateTimeOffset now)
    {
        if (!IncidentStates.IsActive(incident.State)) return false;
        var status = SlaPolicy.Evaluate(incident, now);
        return sla switch
        {
            SlaFilter.ResponseBreached => status.ResponseBreached,
            SlaFilter.ResolutionBreached => status.ResolutionBreached,
            _ => false,
        };
    }

    public static string[] ParseSearchTerms(string? search) =>
        string.IsNullOrWhiteSpace(search)
            ? []
            : search.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<Incident> Sort(IEnumerable<Incident> rows, IncidentSortField field, SortDirection direction)
    {
        var desc = direction == SortDirection.Desc;
        if (field == IncidentSortField.Age)
        {
            // Youngest first is "ascending age", i.e. most recently opened first.
            field = IncidentSortField.Opened;
            desc = !desc;
        }

        var text = StringComparer.OrdinalIgnoreCase;
        var ordered = field switch
        {
            IncidentSortField.Number => By(rows, i => i.Number, desc, text),
            IncidentSortField.Description => By(rows, i => i.ShortDescription, desc, text),
            IncidentSortField.State => By(rows, i => i.State, desc, text),
            IncidentSortField.Group => By(rows, i => i.AssignmentGroup, desc, text),
            IncidentSortField.AssignedBy => By(rows, i => i.AssignedBy, desc, text),
            IncidentSortField.AssignedTo => By(rows, i => i.AssignedTo, desc, text),
            IncidentSortField.Opened => By(rows, i => i.OpenedUtc, desc),
            _ => By(rows, i => (int)i.Priority, desc),
        };
        return ordered.ThenByDescending(i => i.OpenedUtc).ThenBy(i => i.Number, StringComparer.Ordinal);
    }

    private static IOrderedEnumerable<Incident> By<TKey>(
        IEnumerable<Incident> rows, Func<Incident, TKey> key, bool desc, IComparer<TKey>? comparer = null) =>
        desc ? rows.OrderByDescending(key, comparer) : rows.OrderBy(key, comparer);
}
