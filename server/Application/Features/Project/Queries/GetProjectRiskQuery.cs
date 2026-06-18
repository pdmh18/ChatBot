using MediatR;

namespace Application.Features.Project.Queries;

public record GetProjectRiskQuery(Guid ProjectId) : IRequest<object>;
