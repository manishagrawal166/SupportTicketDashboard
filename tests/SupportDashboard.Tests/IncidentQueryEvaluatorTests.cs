using SupportDashboard.Core.Incidents;
using SupportDashboard.Core.Models;

namespace SupportDashboard.Tests;

public class IncidentQueryEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 15, 0, 0, TimeSpan.Zero);

    private static Incident Make(string number, IncidentPriority priority, DateTimeOffset openedUtc,
        string description = "Something broke", string group = "IT Support") => new()
    {
        Number = number,
        ShortDescription = description,
        Priority = priority,
        State = IncidentStates.Open,
        AssignmentGroup = group,
        AssignedBy = "Maria Chen",
        AssignedTo = "Alex Kumar",
        OpenedUtc = openedUtc,
    };

    private static IncidentQueryResult Run(IEnumerable<Incident> incidents, IncidentQuery query) =>
        IncidentQueryEvaluator.Evaluate(incidents, query, Now);

    private static readonly Incident[] Sample =
    [
        Make("INC0000001", IncidentPriority.P1, Now.AddMinutes(-5), "Payment gateway timeout"),
        Make("INC0000002", IncidentPriority.P3, Now.AddHours(-2), "VPN access request"),
        Make("INC0000003", IncidentPriority.P2, Now.AddDays(-1), "SFTP file drop missing"),
        Make("INC0000004", IncidentPriority.P1, Now.AddDays(-3), "Database pool exhaustion"),
    ];

    [Fact]
    public void Search_with_comma_separated_numbers_returns_union_of_matches()
    {
        var result = Run(Sample, new IncidentQuery(Search: "INC0000002, inc0000004"));

        Assert.Equal(["INC0000004", "INC0000002"], result.Items.Select(i => i.Number));
    }

    [Fact]
    public void Search_ignores_blank_terms_and_also_matches_description()
    {
        var result = Run(Sample, new IncidentQuery(Search: " , vpn ,, "));

        Assert.Equal("INC0000002", Assert.Single(result.Items).Number);
    }

    [Fact]
    public void Priority_filter_narrows_items_but_not_totals_or_facets()
    {
        var result = Run(Sample, new IncidentQuery(Priority: IncidentPriority.P1));

        Assert.All(result.Items, i => Assert.Equal(IncidentPriority.P1, i.Priority));
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(4, result.Totals.Total);
        Assert.Equal(4, result.Facets.Total);
    }

    [Fact]
    public void Facets_reflect_search_but_totals_do_not()
    {
        var result = Run(Sample, new IncidentQuery(Search: "INC0000001,INC0000003"));

        Assert.Equal(new PriorityCounts(2, 1, 1, 0, 0, 0), result.Facets);
        Assert.Equal(new PriorityCounts(4, 2, 1, 1, 0, 0), result.Totals);
    }

    [Fact]
    public void Date_range_uses_central_time_day_boundaries()
    {
        // 04:30 UTC on Oct 18 is 11:30 PM CDT on Oct 17.
        var lateEvening = Make("INC0000099", IncidentPriority.P4, new DateTimeOffset(2026, 10, 18, 4, 30, 0, TimeSpan.Zero));
        var oct17 = new DateOnly(2026, 10, 17);
        var oct18 = new DateOnly(2026, 10, 18);

        Assert.Single(Run([lateEvening], new IncidentQuery(From: oct17, To: oct17)).Items);
        Assert.Empty(Run([lateEvening], new IncidentQuery(From: oct18)).Items);
    }

    [Fact]
    public void Default_sort_is_priority_then_newest_first()
    {
        var result = Run(Sample, new IncidentQuery());

        Assert.Equal(["INC0000001", "INC0000004", "INC0000003", "INC0000002"], result.Items.Select(i => i.Number));
    }

    [Fact]
    public void Sorting_by_age_ascending_puts_youngest_first()
    {
        var asc = Run(Sample, new IncidentQuery(Sort: IncidentSortField.Age));
        var desc = Run(Sample, new IncidentQuery(Sort: IncidentSortField.Age, Direction: SortDirection.Desc));

        Assert.Equal("INC0000001", asc.Items[0].Number);
        Assert.Equal("INC0000004", desc.Items[0].Number);
    }

    [Fact]
    public void Response_sla_is_breached_when_unanswered_past_target()
    {
        var p1 = Make("INC0000010", IncidentPriority.P1, Now.AddMinutes(-20));

        Assert.True(SlaPolicy.Evaluate(p1, Now).ResponseBreached);
        Assert.False(SlaPolicy.Evaluate(p1, Now).ResolutionBreached);
    }

    [Fact]
    public void Response_sla_is_met_when_responded_within_target_even_if_now_is_later()
    {
        var p1 = Make("INC0000011", IncidentPriority.P1, Now.AddHours(-2));
        p1.RespondedUtc = p1.OpenedUtc.AddMinutes(10);

        Assert.False(SlaPolicy.Evaluate(p1, Now).ResponseBreached);
    }

    [Fact]
    public void Sla_filter_only_returns_active_breached_incidents()
    {
        var openBreached = Make("INC0000020", IncidentPriority.P2, Now.AddHours(-10));
        var resolvedBreached = Make("INC0000021", IncidentPriority.P2, Now.AddHours(-10));
        resolvedBreached.State = IncidentStates.Resolved;
        resolvedBreached.ResolvedUtc = Now.AddHours(-1);
        var withinTarget = Make("INC0000022", IncidentPriority.P2, Now.AddHours(-1));

        var result = Run([openBreached, resolvedBreached, withinTarget], new IncidentQuery(Sla: SlaFilter.ResolutionBreached));

        Assert.Equal("INC0000020", Assert.Single(result.Items).Number);
    }
}
