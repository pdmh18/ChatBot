using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Auth.Commands;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand cmd)
        => Ok(await _mediator.Send(cmd));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd)
        => Ok(await _mediator.Send(cmd));
}
