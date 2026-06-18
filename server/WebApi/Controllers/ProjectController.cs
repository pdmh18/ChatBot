using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Project.Queries;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProjectController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}/risk")]
    public async Task<IActionResult> GetRisk(Guid id)
        => Ok(await _mediator.Send(new GetProjectRiskQuery(id)));
}
