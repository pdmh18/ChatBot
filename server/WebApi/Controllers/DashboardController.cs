using Application.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int? projectId,
        [FromQuery] int? sprintId,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(
            projectId,
            sprintId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("workload")]
    public async Task<IActionResult> GetWorkload(
        [FromQuery] int? projectId,
        [FromQuery] int? sprintId,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetWorkloadAsync(
            projectId,
            sprintId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] int? projectId,
        [FromQuery] int? sprintId,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetAlertsAsync(
            projectId,
            sprintId,
            type,
            cancellationToken);

        return Ok(result);
    }
}