using SupportDashboard.Core.Incidents;
using SupportDashboard.Core.Models;

namespace SupportDashboard.Web.Models;

/// <param name="SlaKey">Query value for the Incidents page filter: "response" or "resolution".</param>
/// <param name="Targets">Per-priority target, keyed P1..P5, e.g. "15m".</param>
public sealed record SlaCardViewModel(
    string Theme,
    string Title,
    string SlaKey,
    int Breached,
    int ActiveTotal,
    PriorityCounts ByPriority,
    IReadOnlyList<(string Priority, string Target)> Targets)
{
    public double BreachedPercent => ActiveTotal == 0 ? 0 : 100.0 * Breached / ActiveTotal;
}

public sealed record RecentIncidentViewModel(Incident Incident, SlaStatus Sla, bool IsNew);

public sealed record OverviewViewModel(
    IReadOnlyList<SlaCardViewModel> Cards,
    IReadOnlyList<RecentIncidentViewModel> Recent,
    int WindowHours,
    IReadOnlyList<int> WindowOptions,
    DateTimeOffset Now);

public sealed record RunStats(string TotalLabel, int Total, int Succeeded, int Failed, int Running)
{
    public static RunStats From(string totalLabel, IEnumerable<RunStatus> statuses)
    {
        int total = 0, succeeded = 0, failed = 0, running = 0;
        foreach (var status in statuses)
        {
            total++;
            switch (status)
            {
                case RunStatus.Succeeded: succeeded++; break;
                case RunStatus.Failed: failed++; break;
                case RunStatus.Running: running++; break;
            }
        }
        return new RunStats(totalLabel, total, succeeded, failed, running);
    }

    public double SuccessRate => Succeeded + Failed == 0 ? 100 : 100.0 * Succeeded / (Succeeded + Failed);
}

public sealed record RunListViewModel<TRun>(RunStats Stats, IReadOnlyList<TRun> Items, DateTimeOffset Now);

public sealed record IncidentsPageViewModel(PriorityCounts Totals);
