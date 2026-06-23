using Application.Common.DTOs.Ai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Ai
{
    public interface IAiClient
    {
        Task<TaskRiskAiResponse> PredictRiskAsync(
            TaskRiskAiRequest request,
            CancellationToken cancellationToken = default);

        Task<StaffMatchAiResponse> MatchStaffAsync(
            StaffMatchAiRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BottleneckAiResponse>> AnalyzeBottleneckAsync(
            int topN,
            CancellationToken cancellationToken = default);
    }
}
