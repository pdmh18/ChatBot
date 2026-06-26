using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Common.DTOs.Dashboard.DashboardDtos;

namespace Application.Features.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(
        int? projectId,
        int? sprintId,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DashboardWorkloadDto>> GetWorkloadAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DashboardTaskAlertDto>> GetAlertsAsync(
            int? projectId,
            int? sprintId,
            string? type,
            CancellationToken cancellationToken = default);
    }
}
