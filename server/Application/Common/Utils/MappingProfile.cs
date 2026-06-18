using AutoMapper;
using Application.Common.DTOs;
using Domain.Entities;

namespace Application.Common.Utils;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Message, MessageDto>();
        CreateMap<Project, ProjectDto>();
        CreateMap<Conversation, ConversationDto>()
            .ForMember(d => d.Messages, o => o.MapFrom(s => s.Messages.OrderBy(m => m.CreatedAt).ToList()));
    }
}
