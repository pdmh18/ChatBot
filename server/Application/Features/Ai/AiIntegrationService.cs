using Application.Common.DTOs.Ai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Ai
{
    public class AiIntegrationService : IAiIntegrationService
    {
        private const string LateRiskTypeName = "Tre han";
        private const string LateRiskDescription = "Rui ro cong viec co kha nang tre han";

        private readonly IAiClient _aiClient;
        private readonly IAiPredictionRepository _repository;

        public AiIntegrationService(
            IAiClient aiClient,
            IAiPredictionRepository repository)
        {
            _aiClient = aiClient;
            _repository = repository;
        }

        public async Task<RiskPredictionResultDto> PredictTaskRiskAsync(
            int taskId,
            CancellationToken cancellationToken = default)
        {
            var task = await _repository.GetTaskDataAsync(taskId, cancellationToken);

            if (task == null)
            {
                throw new KeyNotFoundException("Không tìm thấy công việc.");
            }

            if (!task.MaNguoiPhuTrach.HasValue)
            {
                throw new InvalidOperationException("Công việc chưa có người phụ trách, không thể dự báo rủi ro.");
            }

            var user = await _repository.GetUserDataAsync(task.MaNguoiPhuTrach.Value, cancellationToken);

            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người phụ trách.");
            }

            var dependencyCount = await _repository.CountPreviousDependenciesAsync(taskId, cancellationToken);

            var aiRequest = new TaskRiskAiRequest
            {
                SoGioUocTinh = ToDouble(task.SoGioUocTinh),
                SoNamKinhNghiemNhanSu = user.SoNamKinhNghiem,
                KhoiLuongHienTaiNhanSu = ToDouble(user.PhanTramTai),
                SoCongViecPhuThuocTruoc = dependencyCount,
                DoUuTienEncoded = EncodePriority(task.DoUuTien)
            };

            var aiResult = await _aiClient.PredictRiskAsync(aiRequest, cancellationToken);

            var riskTypeId = await _repository.GetOrCreateRiskTypeAsync(
                LateRiskTypeName,
                LateRiskDescription,
                cancellationToken);

            return await _repository.SaveRiskPredictionAsync(
                task,
                riskTypeId,
                aiResult,
                cancellationToken);
        }

        public async Task<StaffMatchResultDto> MatchStaffAsync(
            int taskId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var task = await _repository.GetTaskDataAsync(taskId, cancellationToken);

            if (task == null)
            {
                throw new KeyNotFoundException("Không tìm thấy công việc.");
            }

            var match = await BuildStaffMatchSaveItemAsync(
                task,
                userId,
                cancellationToken);

            return await _repository.SaveStaffMatchAsync(
                task,
                match.User,
                match.AiResult,
                cancellationToken);
        }

        public async Task<IReadOnlyList<StaffMatchResultDto>> SuggestAssigneesAsync(
            int taskId,
            CancellationToken cancellationToken = default)
        {
            var task = await _repository.GetTaskDataAsync(taskId, cancellationToken);

            if (task == null)
            {
                throw new KeyNotFoundException("Không tìm thấy công việc.");
            }

            var userIds = await _repository.GetProjectMemberUserIdsAsync(task.MaDuAn, cancellationToken);

            if (userIds.Count == 0)
            {
                throw new InvalidOperationException("Dự án chưa có thành viên để đề xuất giao việc.");
            }

            var suggestions = new List<StaffMatchSaveItemDto>();

            foreach (var userId in userIds)
            {
                var suggestion = await BuildStaffMatchSaveItemAsync(
                    task,
                    userId,
                    cancellationToken);

                suggestions.Add(suggestion);
            }

            var orderedSuggestions = suggestions
                .OrderByDescending(x => x.AiResult.XacSuatHieuQua)
                .ToList();

            return await _repository.ReplaceStaffSuggestionsAsync(
                task,
                orderedSuggestions,
                cancellationToken);
        }

        public async Task<IReadOnlyList<BottleneckResultDto>> AnalyzeBottlenecksAsync(
            int topN,
            CancellationToken cancellationToken = default)
        {
            if (topN < 1 || topN > 100)
            {
                throw new ArgumentException("topN phải nằm trong khoảng 1 đến 100.");
            }

            var aiResults = await _aiClient.AnalyzeBottleneckAsync(topN, cancellationToken);

            return await _repository.SaveBottleneckResultsAsync(aiResults, cancellationToken);
        }

        private async Task<StaffMatchSaveItemDto> BuildStaffMatchSaveItemAsync(
            AiTaskDataDto task,
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _repository.GetUserDataAsync(userId, cancellationToken);

            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhân sự.");
            }

            var aiRequest = new StaffMatchAiRequest
            {
                SoGioUocTinh = ToDouble(task.SoGioUocTinh),
                PhanTramTaiNhanSu = ToDouble(user.PhanTramTai),
                DiemChatLuongTrungBinhLichSu = ToDouble(user.DiemChatLuongTrungBinh)
            };

            var aiResult = await _aiClient.MatchStaffAsync(aiRequest, cancellationToken);

            return new StaffMatchSaveItemDto
            {
                User = user,
                AiResult = aiResult
            };
        }

        private static int EncodePriority(string? priority)
        {
            return priority switch
            {
                "Thap" => 0,
                "Trung binh" => 1,
                "Cao" => 2,
                "Khan cap" => 3,
                _ => 1
            };
        }

        private static double ToDouble(decimal value)
        {
            return Convert.ToDouble(value);
        }
    }
}
