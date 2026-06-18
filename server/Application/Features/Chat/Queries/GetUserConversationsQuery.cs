using MediatR;
using Application.Common.DTOs;

namespace Application.Features.Chat.Queries;

public record GetUserConversationsQuery(Guid UserId) : IRequest<List<ConversationDto>>;
