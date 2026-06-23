using Application.Common.DTOs.Ai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Ai
{
    public interface IAiIntegrationService
    {
        Task<RiskPredictionResultDto> PredictTaskRiskAsync(
            int taskId,
            CancellationToken cancellationToken = default);

        Task<StaffMatchResultDto> MatchStaffAsync(
            int taskId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StaffMatchResultDto>> SuggestAssigneesAsync(
            int taskId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BottleneckResultDto>> AnalyzeBottlenecksAsync(
            int topN,
            CancellationToken cancellationToken = default);
    }
}
