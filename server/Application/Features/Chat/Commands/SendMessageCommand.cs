using MediatR;
using Application.Common.DTOs;

namespace Application.Features.Chat.Commands;

public record SendMessageCommand(Guid UserId, Guid ConversationId, string Content) : IRequest<MessageDto>;
