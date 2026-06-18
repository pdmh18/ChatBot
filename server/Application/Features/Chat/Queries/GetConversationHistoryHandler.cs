using AutoMapper;
using MediatR;
using Application.Common.DTOs;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Features.Chat.Queries;

public class GetConversationHistoryHandler : IRequestHandler<GetConversationHistoryQuery, ConversationDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetConversationHistoryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<ConversationDto> Handle(GetConversationHistoryQuery request, CancellationToken ct)
    {
        var convs = await _uow.Repository<Conversation>()
            .FindAsync(c => c.Id == request.ConversationId && c.UserId == request.UserId);
        var conv = convs.FirstOrDefault() ?? throw new Exception("Conversation not found");
        return _mapper.Map<ConversationDto>(conv);
    }
}
