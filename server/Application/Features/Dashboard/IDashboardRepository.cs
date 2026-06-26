using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Application.Common.DTOs.Dashboard.DashboardDtos;

namespace Application.Features.Dashboard
{
    public interface IDashboardRepository
    {
        Task<IReadOnlyList<DashboardTaskAlertDto>> GetTasksAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DashboardWorkloadDto>> GetWorkloadAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default);

        Task<int> CountLatestBottleneckBatchAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default);
    }
}