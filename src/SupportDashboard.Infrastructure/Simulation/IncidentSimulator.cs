using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;

namespace SupportDashboard.Infrastructure.Simulation;

/// <summary>
/// Creates sample incidents on demand so the new-ticket notification flow can be exercised
/// before the real ServiceNow sync exists. Cycles through every priority level.
/// </summary>
public sealed class IncidentSimulator(IIncidentRepository repository, TimeProvider time)
{
    private static readonly (string Description, IncidentPriority Priority, string Group, string AssignedBy, string AssignedTo)[] Templates =
    [
        ("Checkout API returning 500 errors intermittently", IncidentPriority.P1, "Payments Platform", "Maria Chen", "Alex Kumar"),
        ("Scheduled backup job failed on reporting DB", IncidentPriority.P2, "Database Operations", "Raj Patel", "Jordan Lee"),
        ("Customer portal login redirect loop", IncidentPriority.P3, "Identity & Access", "Maria Chen", "Priya Nair"),
        ("New vendor onboarding form not submitting", IncidentPriority.P4, "Integration Services", "Tom Becker", "Sara Ibrahim"),
        ("Conference room display not pairing", IncidentPriority.P5, "IT Support", "Tom Becker", "Chris Yoon"),
    ];

    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _templateIndex;

    public async Task<Incident> CreateAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var template = Templates[_templateIndex];
            _templateIndex = (_templateIndex + 1) % Templates.Length;

            var existing = await repository.GetAllAsync(ct);
            var nextNumber = existing.Select(i => ParseNumber(i.Number)).DefaultIfEmpty(0).Max() + 1;

            var incident = new Incident
            {
                Number = $"INC{nextNumber:D7}",
                ShortDescription = template.Description,
                Priority = template.Priority,
                State = IncidentStates.Open,
                AssignmentGroup = template.Group,
                AssignedBy = template.AssignedBy,
                AssignedTo = template.AssignedTo,
                OpenedUtc = time.GetUtcNow(),
            };
            await repository.AddAsync(incident, ct);
            return incident;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static int ParseNumber(string number) =>
        number.StartsWith("INC", StringComparison.OrdinalIgnoreCase) && int.TryParse(number.AsSpan(3), out var value)
            ? value
            : 0;
}
