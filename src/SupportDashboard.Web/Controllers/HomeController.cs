using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SupportDashboard.Core.Incidents;
using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;
using SupportDashboard.Web.Formatting;
using SupportDashboard.Web.Models;

namespace SupportDashboard.Web.Controllers;

public sealed class HomeController(IIncidentRepository incidents, TimeProvider time) : Controller
{
    private static readonly int[] WindowOptions = [1, 2, 4, 8];
    private const int DefaultWindowHours = 8;

    public async Task<IActionResult> Index(int? hours, CancellationToken ct)
    {
        var windowHours = hours is { } h && WindowOptions.Contains(h) ? h : DefaultWindowHours;
        var now = time.GetUtcNow();
        var all = await incidents.GetAllAsync(ct);
        var active = all.Where(i => IncidentStates.IsActive(i.State)).ToList();

        var cards = new[]
        {
            BuildCard("sla-response", "Response SLA breached", "response", SlaFilter.ResponseBreached, SlaPolicy.ResponseTarget),
            BuildCard("sla-resolution", "Resolution SLA breached", "resolution", SlaFilter.ResolutionBreached, SlaPolicy.ResolutionTarget),
        };

        var cutoff = now - TimeSpan.FromHours(windowHours);
        var recent = all
            .Where(i => i.OpenedUtc >= cutoff)
            .OrderByDescending(i => i.OpenedUtc)
            .Select(i => new RecentIncidentViewModel(i, SlaPolicy.Evaluate(i, now), DisplayFormat.IsNew(i.OpenedUtc, now)))
            .ToList();

        return View(new OverviewViewModel(cards, recent, windowHours, WindowOptions, now));

        SlaCardViewModel BuildCard(string theme, string title, string key, SlaFilter filter, Func<IncidentPriority, TimeSpan> target)
        {
            var breached = active.Where(i => IncidentQueryEvaluator.IsBreached(i, filter, now)).ToList();
            var targets = Enum.GetValues<IncidentPriority>()
                .Select(p => (p.ToString(), DisplayFormat.ShortSpan(target(p))))
                .ToList();
            return new SlaCardViewModel(theme, title, key, breached.Count, active.Count, PriorityCounts.From(breached), targets);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
