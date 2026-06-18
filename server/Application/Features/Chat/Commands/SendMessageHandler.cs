using AutoMapper;
using MediatR;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Features.Chat.Commands;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _ai;
    private readonly IMapper _mapper;

    public SendMessageHandler(IUnitOfWork uow, IAiService ai, IMapper mapper)
    {
        _uow = uow; _ai = ai; _mapper = mapper;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var conversations = await _uow.Repository<Conversation>()
            .FindAsync(c => c.Id == request.ConversationId && c.UserId == request.UserId);
        var conversation = conversations.FirstOrDefault() ?? throw new Exception("Conversation not found");

        // Save user message
        var userMsg = new Message { ConversationId = request.ConversationId, Role = MessageRole.User, Content = request.Content };
        await _uow.Repository<Message>().AddAsync(userMsg);

        // Build history for AI
        var messages = await _uow.Repository<Message>().FindAsync(m => m.ConversationId == request.ConversationId);
        var history = messages.OrderBy(m => m.CreatedAt)
            .Select(m => (m.Role.ToString().ToLower(), m.Content)).ToList();

        // Call AI
        var aiReply = await _ai.ChatAsync(request.Content, history);

        // Save AI message
        var aiMsg = new Message { ConversationId = request.ConversationId, Role = MessageRole.Assistant, Content = aiReply };
        await _uow.Repository<Message>().AddAsync(aiMsg);
        await _uow.SaveChangesAsync();

        return _mapper.Map<MessageDto>(aiMsg);
    }
}
