using Application.Common.DTOs.Ai;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Ai
{
    public interface IAiPredictionRepository
    {
        Task<AiTaskDataDto?> GetTaskDataAsync(
            int taskId,
            CancellationToken cancellationToken = default);

        Task<AiUserDataDto?> GetUserDataAsync(
            int userId,
            int projectId,
            int? sprintId,
            int? excludedTaskId,
            CancellationToken cancellationToken = default);

        Task<int> CountPreviousDependenciesAsync(
            int taskId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<int>> GetProjectMemberUserIdsAsync(
            int projectId,
            CancellationToken cancellationToken = default);

        Task<int> GetOrCreateRiskTypeAsync(
            string riskTypeName,
            string? description,
            CancellationToken cancellationToken = default);

        Task<RiskPredictionResultDto> SaveRiskPredictionAsync(
            AiTaskDataDto task,
            int riskTypeId,
            TaskRiskAiResponse aiResult,
            CancellationToken cancellationToken = default);

        Task<StaffMatchResultDto> SaveStaffMatchAsync(
            AiTaskDataDto task,
            AiUserDataDto user,
            StaffMatchAiResponse aiResult,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StaffMatchResultDto>> ReplaceStaffSuggestionsAsync(
            AiTaskDataDto task,
            IReadOnlyList<StaffMatchSaveItemDto> suggestions,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BottleneckResultDto>> SaveBottleneckResultsAsync(
            IReadOnlyList<BottleneckAiResponse> aiResults,
            CancellationToken cancellationToken = default);
    }
}