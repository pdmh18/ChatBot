using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Common.DTOs.Dashboard.DashboardDtos;

namespace Application.Features.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const string StatusDone = "Hoan thanh";
        private const string StatusCanceled = "Da huy";

        private const string PriorityMedium = "Trung binh";
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
                cancellationToken);

            return new DashboardSummaryDto
            {
                TongCongViec = tasks.Count,
                TaskNguyCoTreHan = tasks.Count(x => x.RiskPercent >= 70),
                DiemNghen = bottleneckCount,
                TaskChuaPhanCong = tasks.Count(x => !x.MaNguoiPhuTrach.HasValue),
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
            var tasks = await _repository.GetTasksAsync(
                projectId,
                sprintId,
                cancellationToken);

            var activeTasks = tasks
                .Where(x =>
                    x.MaNguoiPhuTrach.HasValue &&
                    !IsSame(x.TrangThai, StatusDone) &&
                    !IsSame(x.TrangThai, StatusCanceled))
                .ToList();

            return activeTasks
                .GroupBy(x => new
                {
                    MaNguoiDung = x.MaNguoiPhuTrach!.Value,
                    HoTen = x.NguoiPhuTrach ?? "Khong ro"
                })
                .Select(g =>
                {
                    var tongGio = g.Sum(x => x.SoGioUocTinh ?? 0m);

                   
                    // 60h được xem là 100% tải trong sprint.
                    var phanTramTai = Math.Round((tongGio / 60m) * 100m, 2);

                    return new DashboardWorkloadDto
                    {
                        MaNguoiDung = g.Key.MaNguoiDung,
                        HoTen = g.Key.HoTen,
                        SoTask = g.Count(),
                        TongGioUocTinh = tongGio,
                        PhanTramTai = phanTramTai,
                        MucDoTai = GetWorkloadLevel(phanTramTai)
                    };
                })
                .OrderByDescending(x => x.TongGioUocTinh)
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
                .Where(x => x.RiskPercent >= 70)
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
                    task.HanChot,
                    task.TrangThai,
                    task.DoUuTien,
                    task.TienDo);

                task.RiskLevel = GetRiskLevel(task.RiskPercent);
            }
        }

        private static int CalculateRisk(
            DateOnly? deadline,
            string? status,
            string? priority,
            int? progress)
        {
            if (IsSame(status, StatusDone))
            {
                return 0;
            }

            var score = 0;
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (deadline.HasValue)
            {
                var daysLeft = deadline.Value.DayNumber - today.DayNumber;

                if (daysLeft < 0)
                {
                    score += 45;
                }
                else if (daysLeft <= 3)
                {
                    score += 25;
                }
                else if (daysLeft <= 7)
                {
                    score += 10;
                }
            }

            if (IsSame(priority, PriorityUrgent))
            {
                score += 35;
            }
            else if (IsSame(priority, PriorityHigh))
            {
                score += 25;
            }
            else if (IsSame(priority, PriorityMedium))
            {
                score += 10;
            }

            var taskProgress = progress ?? 0;

            if (taskProgress < 30)
            {
                score += 20;
            }
            else if (taskProgress < 60)
            {
                score += 10;
            }

            return Math.Clamp(score, 0, 100);
        }

        private static string GetRiskLevel(int riskPercent)
        {
            if (riskPercent >= 70) return "Cao";
            if (riskPercent >= 31) return "Trung binh";
            return "Thap";
        }

        private static string GetWorkloadLevel(decimal phanTramTai)
        {
            if (phanTramTai >= 100) return "Qua tai";
            if (phanTramTai >= 80) return "Cao";
            if (phanTramTai >= 50) return "Trung binh";
            return "Thap";
        }

        private static string BuildLateRiskReason(DashboardTaskAlertDto task)
        {
            var reasons = new List<string>();

            if (task.HanChot.HasValue)
            {
                var daysLeft = task.HanChot.Value.DayNumber -
                               DateOnly.FromDateTime(DateTime.Today).DayNumber;

                if (daysLeft < 0)
                {
                    reasons.Add("task đã quá hạn");
                }
                else if (daysLeft <= 3)
                {
                    reasons.Add("task sắp đến hạn");
                }
            }

            if ((task.TienDo ?? 0) < 30)
            {
                reasons.Add("tiến độ còn thấp");
            }

            if (IsSame(task.DoUuTien, PriorityHigh) ||
                IsSame(task.DoUuTien, PriorityUrgent))
            {
                reasons.Add("độ ưu tiên cao");
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
