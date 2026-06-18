namespace Application.Common.DTOs;

public record ConversationDto(Guid Id, string Title, Guid? ProjectId, DateTime CreatedAt, List<MessageDto> Messages);
