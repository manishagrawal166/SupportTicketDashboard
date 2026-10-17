using Microsoft.AspNetCore.Mvc;
using SupportDashboard.Core.Incidents;
using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;
using SupportDashboard.Infrastructure.Simulation;
using SupportDashboard.Web.Models;

namespace SupportDashboard.Web.Controllers.Api;

[ApiController]
[Route("api/incidents")]
public sealed class IncidentsApiController(
    IIncidentRepository incidents,
    IncidentSimulator simulator,
    TimeProvider time) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IncidentListResponse>> Get(
        [FromQuery] string? priority,
        [FromQuery] string? search,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sort,
        [FromQuery] string? dir,
        [FromQuery] string? sla,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var query = new IncidentQuery(ParsePriority(priority), search, from, to, ParseSort(sort), ParseDirection(dir), ParseSla(sla));
        var result = IncidentQueryEvaluator.Evaluate(await incidents.GetAllAsync(ct), query, now);

        return new IncidentListResponse(
            result.Totals,
            result.Facets,
            result.Items.Select(i => IncidentDto.From(i, now)).ToList(),
            now);
    }

    [HttpPost("simulate")]
    public async Task<ActionResult<IncidentDto>> Simulate(CancellationToken ct)
    {
        var incident = await simulator.CreateAsync(ct);
        return IncidentDto.From(incident, time.GetUtcNow());
    }

    [HttpPut("{number}/comment")]
    public async Task<IActionResult> UpdateComment(string number, [FromBody] CommentRequest request, CancellationToken ct)
    {
        return await incidents.UpdateCommentAsync(number, request.Comment, ct) ? NoContent() : NotFound();
    }

    private static IncidentPriority? ParsePriority(string? value) =>
        Enum.TryParse<IncidentPriority>(value, ignoreCase: true, out var p) && Enum.IsDefined(p) ? p : null;

    private static IncidentSortField ParseSort(string? value) =>
        Enum.TryParse<IncidentSortField>(value, ignoreCase: true, out var f) && Enum.IsDefined(f) ? f : IncidentSortField.Priority;

    private static SlaFilter ParseSla(string? value) => value?.ToLowerInvariant() switch
    {
        "response" => SlaFilter.ResponseBreached,
        "resolution" => SlaFilter.ResolutionBreached,
        _ => SlaFilter.None,
    };

    private static SortDirection ParseDirection(string? value) =>
        string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase) ? SortDirection.Desc : SortDirection.Asc;
}
