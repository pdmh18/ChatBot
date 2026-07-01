using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Utils;
using static Application.Common.DTOs.Dashboard.DashboardDtos;

namespace Application.Features.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const string StatusDone = "Hoan thanh";
        private const string StatusCanceled = "Da huy";
        private const string StatusTodo = "Can lam";
        private const string StatusBlocked = "Bi chan";
        private const string StatusRejected = "Bi tu choi";

        private const string PriorityHigh = "Cao";
        private const string PriorityUrgent = "Khan cap";

        private readonly IDashboardRepository _repository;

        public DashboardService(IDashboardRepository repository)
        {
            _repository = repository;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _repository.GetTasksAsync(
                projectId,
                sprintId,
                cancellationToken);

            ApplyRisk(tasks);

            var bottleneckCount = await _repository.CountLatestBottleneckBatchAsync(
                projectId,
                sprintId,
                cancellationToken);

            return new DashboardSummaryDto
            {
                TongCongViec = tasks.Count(x => !IsSame(x.TrangThai, StatusCanceled)),
                TaskNguyCoTreHan = tasks.Count(x => x.RiskPercent >= 70),
                DiemNghen = bottleneckCount,
                TaskChuaPhanCong = tasks.Count(x =>
                    !x.MaNguoiPhuTrach.HasValue &&
                    !IsSame(x.TrangThai, StatusCanceled)),
                TaskHoanThanh = tasks.Count(x => IsSame(x.TrangThai, StatusDone)),
                TaskDangLam = tasks.Count(x =>
                    !IsSame(x.TrangThai, StatusDone) &&
                    !IsSame(x.TrangThai, StatusCanceled))
            };
        }

        public async Task<IReadOnlyList<DashboardWorkloadDto>> GetWorkloadAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default)
        {
            var workload = await _repository.GetWorkloadAsync(
                projectId,
                sprintId,
                cancellationToken);

            foreach (var item in workload)
            {
                item.PhanTramTai = Math.Round(item.PhanTramTai, 2);
                item.MucDoTai = GetWorkloadLevel(item.PhanTramTai);
            }

            return workload
                .OrderByDescending(x => x.PhanTramTai)
                .ToList();
        }

        public async Task<IReadOnlyList<DashboardTaskAlertDto>> GetAlertsAsync(
            int? projectId,
            int? sprintId,
            string? type,
            CancellationToken cancellationToken = default)
        {
            var normalizedType = NormalizeType(type);

            var tasks = await _repository.GetTasksAsync(
                projectId,
                sprintId,
                cancellationToken);

            ApplyRisk(tasks);

            // Bottleneck lấy từ AI bằng POST /api/ai/bottlenecks/analyze?topN=10.
            if (normalizedType == "bottleneck")
            {
                return Array.Empty<DashboardTaskAlertDto>();
            }

            return tasks
                .Where(x =>
                    x.RiskPercent >= 70 &&
                    !IsSame(x.TrangThai, StatusCanceled))
                .Select(x =>
                {
                    x.LoaiCanhBao = "Tre han";
                    x.NguyenNhan = BuildLateRiskReason(x);
                    x.KhuyenNghi = "Cần kiểm tra tiến độ, hỗ trợ thêm nhân sự hoặc điều chỉnh deadline.";
                    return x;
                })
                .OrderByDescending(x => x.RiskPercent)
                .ToList();
        }

        private static void ApplyRisk(IReadOnlyList<DashboardTaskAlertDto> tasks)
        {
            foreach (var task in tasks)
            {
                task.RiskPercent = CalculateRisk(
                    task.NgayBatDau,
                    task.HanChot,
                    task.TrangThai,
                    task.DoUuTien,
                    task.TienDo,
                    task.SoGioUocTinh,
                    task.MaNguoiPhuTrach);

                task.RiskLevel = GetRiskLevel(task.RiskPercent);
            }
        }

        private static int CalculateRisk(
            DateOnly? startDate,
            DateOnly? deadline,
            string? status,
            string? priority,
            int? progress,
            decimal? estimatedHours,
            int? assigneeId)
        {
            return TaskRiskCalculator.Calculate(
                startDate,
                deadline,
                status,
                priority,
                progress,
                estimatedHours,
                assigneeId);
        }

        private static string GetRiskLevel(int riskPercent)
        {
            return TaskRiskCalculator.GetVietnameseRiskLevel(riskPercent);
        }

        private static string GetWorkloadLevel(decimal phanTramTai)
        {
            if (phanTramTai >= 85) return "Qua tai";
            if (phanTramTai >= 70) return "Cao";
            if (phanTramTai >= 40) return "Trung binh";
            return "Thap";
        }

        private static string BuildLateRiskReason(DashboardTaskAlertDto task)
        {
            var reasons = new List<string>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (task.HanChot.HasValue)
            {
                var daysLeft = task.HanChot.Value.DayNumber - today.DayNumber;

                if (daysLeft < 0)
                {
                    reasons.Add($"task đã quá hạn {Math.Abs(daysLeft)} ngày");
                }
                else if (daysLeft == 0)
                {
                    reasons.Add("task đến hạn hôm nay");
                }
                else if (daysLeft <= 3)
                {
                    reasons.Add($"task sắp đến hạn trong {daysLeft} ngày");
                }
                else if (daysLeft <= 7)
                {
                    reasons.Add($"deadline còn {daysLeft} ngày");
                }
            }

            var progress = Math.Clamp(task.TienDo ?? 0, 0, 100);
            var hasStarted = !task.NgayBatDau.HasValue || today >= task.NgayBatDau.Value;
            var expectedProgress = TaskRiskCalculator.CalculateExpectedProgress(
                task.NgayBatDau,
                task.HanChot,
                today);

            if (expectedProgress.HasValue && expectedProgress.Value - progress >= 20)
            {
                reasons.Add($"tiến độ thấp hơn kỳ vọng khoảng {expectedProgress.Value - progress}%");
            }
            else if (hasStarted && progress < 30)
            {
                reasons.Add("tiến độ còn thấp");
            }

            if (IsSame(task.TrangThai, StatusBlocked))
            {
                reasons.Add("task đang bị chặn");
            }
            else if (IsSame(task.TrangThai, StatusRejected))
            {
                reasons.Add("task bị từ chối, cần xử lý lại");
            }
            else if (IsSame(task.TrangThai, StatusTodo) &&
                     task.NgayBatDau.HasValue &&
                     today > task.NgayBatDau.Value)
            {
                reasons.Add("task đã qua ngày bắt đầu nhưng vẫn chưa làm");
            }

            if (!task.MaNguoiPhuTrach.HasValue)
            {
                reasons.Add("task chưa có người phụ trách");
            }

            if (IsSame(task.DoUuTien, PriorityHigh) ||
                IsSame(task.DoUuTien, PriorityUrgent))
            {
                reasons.Add("độ ưu tiên cao");
            }

            if ((task.SoGioUocTinh ?? 0m) >= 24m)
            {
                reasons.Add("khối lượng ước tính lớn");
            }

            if (reasons.Count == 0)
            {
                reasons.Add("task có chỉ số rủi ro cao");
            }

            return "Nguyên nhân: " + string.Join(", ", reasons) + ".";
        }

        private static string NormalizeType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return "late-risk";
            }

            return type.Trim().ToLowerInvariant();
        }

        private static bool IsSame(string? value, string expected)
        {
            return string.Equals(
                value?.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
