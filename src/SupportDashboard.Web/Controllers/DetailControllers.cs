using Microsoft.AspNetCore.Mvc;
using SupportDashboard.Core.Incidents;
using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;
using SupportDashboard.Web.Models;

namespace SupportDashboard.Web.Controllers;

public sealed class IncidentsController(IIncidentRepository incidents) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var all = await incidents.GetAllAsync(ct);
        return View(new IncidentsPageViewModel(PriorityCounts.From(all)));
    }
}

public sealed class FunctionAppsController(IRunRepository<FunctionExecution> runs, TimeProvider time) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = (await runs.GetAllAsync(ct)).OrderByDescending(r => r.StartUtc).ToList();
        var stats = RunStats.From("Executions (24h)", items.Select(r => r.Status));
        return View(new RunListViewModel<FunctionExecution>(stats, items, time.GetUtcNow()));
    }
}

public sealed class PipelinesController(IRunRepository<PipelineRun> runs, TimeProvider time) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = (await runs.GetAllAsync(ct)).OrderByDescending(r => r.StartUtc).ToList();
        var stats = RunStats.From("Pipeline runs (24h)", items.Select(r => r.Status));
        return View(new RunListViewModel<PipelineRun>(stats, items, time.GetUtcNow()));
    }
}

public sealed class LogicAppsController(IRunRepository<WorkflowRun> runs, TimeProvider time) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = (await runs.GetAllAsync(ct)).OrderByDescending(r => r.StartUtc).ToList();
        var stats = RunStats.From("Workflow runs (24h)", items.Select(r => r.Status));
        return View(new RunListViewModel<WorkflowRun>(stats, items, time.GetUtcNow()));
    }
}
