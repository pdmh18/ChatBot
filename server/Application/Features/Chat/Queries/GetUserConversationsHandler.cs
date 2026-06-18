using AutoMapper;
using MediatR;
using Application.Common.DTOs;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Features.Chat.Queries;

public class GetUserConversationsHandler : IRequestHandler<GetUserConversationsQuery, List<ConversationDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetUserConversationsHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<List<ConversationDto>> Handle(GetUserConversationsQuery request, CancellationToken ct)
    {
        var convs = await _uow.Repository<Conversation>().FindAsync(c => c.UserId == request.UserId);
        return _mapper.Map<List<ConversationDto>>(convs.OrderByDescending(c => c.CreatedAt).ToList());
    }
}
