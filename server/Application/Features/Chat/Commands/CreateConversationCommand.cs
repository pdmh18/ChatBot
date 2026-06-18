using MediatR;
using Application.Common.DTOs;

namespace Application.Features.Chat.Commands;

public record CreateConversationCommand(Guid UserId, Guid? ProjectId, string Title = "New Chat") : IRequest<ConversationDto>;
