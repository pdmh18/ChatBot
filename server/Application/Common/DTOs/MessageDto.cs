using Domain.Enums;
namespace Application.Common.DTOs;

public record MessageDto(Guid Id, MessageRole Role, string Content, DateTime CreatedAt);
