using AutoMapper;
using MediatR;
using Application.Common.DTOs;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Features.Chat.Commands;

public class CreateConversationHandler : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CreateConversationHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken ct)
    {
        var conv = new Conversation { UserId = request.UserId, ProjectId = request.ProjectId, Title = request.Title };
        await _uow.Repository<Conversation>().AddAsync(conv);
        await _uow.SaveChangesAsync();
        return _mapper.Map<ConversationDto>(conv);
    }
}
