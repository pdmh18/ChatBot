using MediatR;
using Application.Common.DTOs;

namespace Application.Features.Chat.Queries;

public record GetConversationHistoryQuery(Guid ConversationId, Guid UserId) : IRequest<ConversationDto>;
