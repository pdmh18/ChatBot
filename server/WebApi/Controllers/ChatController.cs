using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Chat.Commands;
using Application.Features.Chat.Queries;
using System.Security.Claims;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    public ChatController(IMediator mediator) => _mediator = mediator;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationCommand cmd)
        => Ok(await _mediator.Send(cmd with { UserId = UserId }));

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
        => Ok(await _mediator.Send(new GetUserConversationsQuery(UserId)));

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetHistory(Guid id)
        => Ok(await _mediator.Send(new GetConversationHistoryQuery(id, UserId)));

    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] string content)
        => Ok(await _mediator.Send(new SendMessageCommand(UserId, id, content)));
}
