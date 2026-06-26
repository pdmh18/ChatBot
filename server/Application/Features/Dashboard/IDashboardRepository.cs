using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        Task<int> CountLatestBottleneckBatchAsync(
            int? projectId,
            CancellationToken cancellationToken = default);
    }
}
