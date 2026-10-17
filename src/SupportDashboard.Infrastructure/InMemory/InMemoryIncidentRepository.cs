using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;

namespace SupportDashboard.Infrastructure.InMemory;

/// <summary>Sample-data store used until the ServiceNow sync and SQLite database exist.</summary>
public sealed class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly Lock _gate = new();
    private readonly List<Incident> _incidents;

    public InMemoryIncidentRepository(TimeProvider time)
    {
        _incidents = Seed(time.GetUtcNow());
    }

    public Task<IReadOnlyList<Incident>> GetAllAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Incident>>(_incidents.ToList());
        }
    }

    public Task AddAsync(Incident incident, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_incidents.Any(i => string.Equals(i.Number, incident.Number, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Incident {incident.Number} already exists.");
            }
            _incidents.Add(incident);
        }
        return Task.CompletedTask;
    }

    public Task<bool> UpdateCommentAsync(string number, string? comment, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var incident = _incidents.FirstOrDefault(i => string.Equals(i.Number, number, StringComparison.OrdinalIgnoreCase));
            if (incident is null)
            {
                return Task.FromResult(false);
            }
            incident.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            return Task.FromResult(true);
        }
    }

    private static List<Incident> Seed(DateTimeOffset now)
    {
        // respondedAfter/resolvedAfter are offsets from the opened time; null means "not yet".
        Incident Make(string number, string description, IncidentPriority priority, string state,
            string group, string assignedBy, string assignedTo, TimeSpan age,
            TimeSpan? respondedAfter = null, TimeSpan? resolvedAfter = null)
        {
            var opened = now - age;
            return new Incident
            {
                Number = number,
                ShortDescription = description,
                Priority = priority,
                State = state,
                AssignmentGroup = group,
                AssignedBy = assignedBy,
                AssignedTo = assignedTo,
                OpenedUtc = opened,
                RespondedUtc = opened + respondedAfter,
                ResolvedUtc = opened + resolvedAfter,
            };
        }

        static TimeSpan Min(double minutes) => TimeSpan.FromMinutes(minutes);
        static TimeSpan Hrs(double hours) => TimeSpan.FromHours(hours);

        return
        [
            Make("INC0045231", "Payment gateway timeout causing checkout failures for customers on the EU storefront", IncidentPriority.P1, IncidentStates.Open, "Payments Platform", "Maria Chen", "Alex Kumar", Min(6)),
            Make("INC0045229", "VPN access request stuck in approval queue", IncidentPriority.P3, IncidentStates.InProgress, "Network Services", "Raj Patel", "Jordan Lee", Min(41), respondedAfter: Min(20)),
            Make("INC0045224", "SFTP file drop missing for nightly vendor feed", IncidentPriority.P2, IncidentStates.Open, "Integration Services", "Maria Chen", "Priya Nair", Hrs(1.6)),
            Make("INC0045218", "Azure AD sign-in failures for EU region", IncidentPriority.P1, IncidentStates.Open, "Identity & Access", "Tom Becker", "Sara Ibrahim", Hrs(3.4)),
            Make("INC0045207", "Report subscription emails not delivering", IncidentPriority.P5, IncidentStates.InProgress, "BI & Reporting", "Raj Patel", "Dan Wells", Hrs(5.4), respondedAfter: Hrs(3)),
            Make("INC0045201", "Intermittent Wi-Fi drops on floor 3 of the Dallas office", IncidentPriority.P3, IncidentStates.Open, "Network Services", "Tom Becker", "Jordan Lee", Hrs(7.5)),
            Make("INC0045193", "Order sync delay between CRM and ERP", IncidentPriority.P2, IncidentStates.Open, "Integration Services", "Maria Chen", "Priya Nair", Hrs(26), respondedAfter: Min(25)),
            Make("INC0045180", "Printer queue backed up at warehouse 4", IncidentPriority.P5, IncidentStates.Resolved, "IT Support", "Tom Becker", "Chris Yoon", Hrs(30), respondedAfter: Hrs(1), resolvedAfter: Hrs(20)),
            Make("INC0045177", "SharePoint site permissions not syncing for new hires", IncidentPriority.P4, IncidentStates.InProgress, "IT Support", "Raj Patel", "Chris Yoon", Hrs(38), respondedAfter: Hrs(5)),
            Make("INC0045172", "Database connection pool exhaustion on reporting DB", IncidentPriority.P1, IncidentStates.Open, "Database Operations", "Maria Chen", "Alex Kumar", Hrs(50), respondedAfter: Min(10)),
            Make("INC0045166", "Mobile app push notifications delayed", IncidentPriority.P3, IncidentStates.InProgress, "Mobile Engineering", "Raj Patel", "Jordan Lee", Hrs(55), respondedAfter: Hrs(1)),
            Make("INC0045150", "Expense approval workflow stuck at manager step", IncidentPriority.P4, IncidentStates.Open, "Integration Services", "Tom Becker", "Sara Ibrahim", Hrs(80)),
        ];
    }
}
