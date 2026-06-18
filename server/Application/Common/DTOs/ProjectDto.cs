using Domain.Enums;
namespace Application.Common.DTOs;

public record ProjectDto(Guid Id, string Name, string Description, ProjectStatus Status, DateTime? StartDate, DateTime? EndDate);
