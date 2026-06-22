using Application.Features.Lookups;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetProjectsAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetUsersAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("sprints")]
    public async Task<IActionResult> GetSprints(
        [FromQuery] int? projectId,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetSprintsAsync(projectId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetRolesAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("skills")]
    public async Task<IActionResult> GetSkills(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetSkillsAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("task-statuses")]
    public async Task<IActionResult> GetTaskStatuses()
    {
        var result = await _lookupService.GetTaskStatusesAsync();

        return Ok(result);
    }

    [HttpGet("task-priorities")]
    public async Task<IActionResult> GetTaskPriorities()
    {
        var result = await _lookupService.GetTaskPrioritiesAsync();

        return Ok(result);
    }
}