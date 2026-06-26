using Application.Common.DTOs.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Tasks
{
    public class TaskService : ITaskService
    {
        private const string PriorityLow = "Thap";
        private const string PriorityMedium = "Trung binh";
        private const string PriorityHigh = "Cao";
        private const string PriorityUrgent = "Khan cap";

        private const string StatusTodo = "Can lam";
        private const string StatusDoing = "Dang lam";
        private const string StatusReview = "Cho kiem tra";
        private const string StatusDone = "Hoan thanh";
        private const string StatusBlocked = "Bi chan";
        private const string StatusWaitingApproval = "Cho duyet";
        private const string StatusApproved = "Da duyet";
        private const string StatusRejected = "Bi tu choi";
        private const string StatusCanceled = "Da huy";

        private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
        {
            PriorityLow,
            PriorityMedium,
            PriorityHigh,
            PriorityUrgent
        };

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            StatusTodo,
            StatusDoing,
            StatusReview,
            StatusDone,
            StatusBlocked,
            StatusWaitingApproval,
            StatusApproved,
            StatusRejected,
            StatusCanceled
        };

        private readonly ITaskRepository _taskRepository;

        public TaskService(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async Task<IReadOnlyList<TaskListItemDto>> GetListAsync(
            TaskQueryParameters query,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetListAsync(query, cancellationToken);

            foreach (var task in tasks)
            {
                ApplyRiskScore(task);
            }

            return tasks;
        }

        public async Task<TaskDetailDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdAsync(id, cancellationToken);

            if (task == null)
            {
                return null;
            }

            ApplyRiskScore(task);

            return task;
        }

        public async Task<int> CreateAsync(
            CreateTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            NormalizeCreateRequest(request);

            await ValidateCreateAsync(request, cancellationToken);

            return await _taskRepository.CreateAsync(request, cancellationToken);
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            NormalizeUpdateRequest(request);

            await ValidateUpdateAsync(request, cancellationToken);

            return await _taskRepository.UpdateAsync(id, request, cancellationToken);
        }

        public Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _taskRepository.DeleteAsync(id, cancellationToken);
        }

        public async Task<bool> UpdateStatusAsync(
            int id,
            UpdateTaskStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            request.TrangThai = request.TrangThai?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(request.TrangThai))
            {
                throw new ArgumentException("Trạng thái công việc là bắt buộc.");
            }

            if (!AllowedStatuses.Contains(request.TrangThai))
            {
                throw new ArgumentException(
                    $"Trạng thái không hợp lệ. Giá trị hợp lệ: {string.Join(", ", AllowedStatuses)}.");
            }

            if (request.TienDo is < 0 or > 100)
            {
                throw new ArgumentException("Tiến độ phải nằm trong khoảng 0 đến 100.");
            }

            return await _taskRepository.UpdateStatusAsync(id, request, cancellationToken);
        }

        public async Task<bool> AssignAsync(
            int id,
            AssignTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.MaNguoiPhuTrach.HasValue)
            {
                var userExists = await _taskRepository.UserExistsAsync(
                    request.MaNguoiPhuTrach.Value,
                    cancellationToken);

                if (!userExists)
                {
                    throw new ArgumentException("Người phụ trách không tồn tại.");
                }
            }

            return await _taskRepository.AssignAsync(id, request, cancellationToken);
        }

        private async Task ValidateCreateAsync(
            CreateTaskRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TenCongViec))
            {
                throw new ArgumentException("Tên công việc là bắt buộc.");
            }

            ValidatePriority(request.DoUuTien);
            ValidateStatus(request.TrangThai);
            ValidateEstimatedHours(request.SoGioUocTinh);
            ValidateProgress(request.TienDo);
            ValidateDates(request.NgayBatDau, request.HanChot, ngayHoanThanh: null);

            var projectExists = await _taskRepository.ProjectExistsAsync(request.MaDuAn, cancellationToken);

            if (!projectExists)
            {
                throw new ArgumentException("Dự án không tồn tại.");
            }

            var creatorExists = await _taskRepository.UserExistsAsync(request.MaNguoiTao, cancellationToken);

            if (!creatorExists)
            {
                throw new ArgumentException("Người tạo không tồn tại.");
            }

            if (request.MaNguoiPhuTrach.HasValue)
            {
                var assigneeExists = await _taskRepository.UserExistsAsync(
                    request.MaNguoiPhuTrach.Value,
                    cancellationToken);

                if (!assigneeExists)
                {
                    throw new ArgumentException("Người phụ trách không tồn tại.");
                }
            }

            if (request.MaSprint.HasValue)
            {
                var validSprint = await _taskRepository.SprintBelongsToProjectAsync(
                    request.MaSprint.Value,
                    request.MaDuAn,
                    cancellationToken);

                if (!validSprint)
                {
                    throw new ArgumentException("Sprint không thuộc dự án đã chọn.");
                }
            }
        }

        private async Task ValidateUpdateAsync(
            UpdateTaskRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TenCongViec))
            {
                throw new ArgumentException("Tên công việc là bắt buộc.");
            }

            ValidatePriority(request.DoUuTien);
            ValidateStatus(request.TrangThai);
            ValidateEstimatedHours(request.SoGioUocTinh);
            ValidateActualHours(request.SoGioThucTe);
            ValidateProgress(request.TienDo);
            ValidateDates(request.NgayBatDau, request.HanChot, request.NgayHoanThanh);

            var projectExists = await _taskRepository.ProjectExistsAsync(request.MaDuAn, cancellationToken);

            if (!projectExists)
            {
                throw new ArgumentException("Dự án không tồn tại.");
            }

            if (request.MaNguoiPhuTrach.HasValue)
            {
                var assigneeExists = await _taskRepository.UserExistsAsync(
                    request.MaNguoiPhuTrach.Value,
                    cancellationToken);

                if (!assigneeExists)
                {
                    throw new ArgumentException("Người phụ trách không tồn tại.");
                }
            }

            if (request.MaSprint.HasValue)
            {
                var validSprint = await _taskRepository.SprintBelongsToProjectAsync(
                    request.MaSprint.Value,
                    request.MaDuAn,
                    cancellationToken);

                if (!validSprint)
                {
                    throw new ArgumentException("Sprint không thuộc dự án đã chọn.");
                }
            }
        }
        private static string GenerateTaskCode()
        {
            var suffix = Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant();

            return $"TASK-{suffix}";
        }
        private static void NormalizeCreateRequest(CreateTaskRequest request)
        {
            request.MaCongViecCode = string.IsNullOrWhiteSpace(request.MaCongViecCode)
    ? GenerateTaskCode()
    : request.MaCongViecCode.Trim();
            request.TenCongViec = request.TenCongViec?.Trim() ?? string.Empty;
            request.MaCongViecCode = request.MaCongViecCode?.Trim();

            request.DoUuTien = string.IsNullOrWhiteSpace(request.DoUuTien)
                ? PriorityMedium
                : request.DoUuTien.Trim();

            request.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                ? StatusTodo
                : request.TrangThai.Trim();

            request.TienDo ??= 0;
        }

        private static void NormalizeUpdateRequest(UpdateTaskRequest request)
        {
            request.TenCongViec = request.TenCongViec?.Trim() ?? string.Empty;
            request.MaCongViecCode = request.MaCongViecCode?.Trim();

            request.DoUuTien = string.IsNullOrWhiteSpace(request.DoUuTien)
                ? PriorityMedium
                : request.DoUuTien.Trim();

            request.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                ? StatusTodo
                : request.TrangThai.Trim();

            request.TienDo ??= 0;
        }

        private static void ValidatePriority(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
            {
                throw new ArgumentException("Độ ưu tiên là bắt buộc.");
            }

            if (!AllowedPriorities.Contains(priority))
            {
                throw new ArgumentException(
                    $"Độ ưu tiên không hợp lệ. Giá trị hợp lệ: {string.Join(", ", AllowedPriorities)}.");
            }
        }

        private static void ValidateStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Trạng thái công việc là bắt buộc.");
            }

            if (!AllowedStatuses.Contains(status))
            {
                throw new ArgumentException(
                    $"Trạng thái không hợp lệ. Giá trị hợp lệ: {string.Join(", ", AllowedStatuses)}.");
            }
        }

        private static void ValidateEstimatedHours(decimal? estimatedHours)
        {
            if (estimatedHours.HasValue && estimatedHours.Value <= 0)
            {
                throw new ArgumentException("Số giờ ước tính phải lớn hơn 0.");
            }
        }

        private static void ValidateActualHours(decimal? actualHours)
        {
            if (actualHours.HasValue && actualHours.Value <= 0)
            {
                throw new ArgumentException("Số giờ thực tế phải lớn hơn 0.");
            }
        }

        private static void ValidateProgress(int? progress)
        {
            if (progress is < 0 or > 100)
            {
                throw new ArgumentException("Tiến độ phải nằm trong khoảng 0 đến 100.");
            }
        }

        private static void ValidateDates(
            DateOnly? ngayBatDau,
            DateOnly? hanChot,
            DateOnly? ngayHoanThanh)
        {
            if (ngayBatDau.HasValue && hanChot.HasValue && hanChot.Value <= ngayBatDau.Value)
            {
                throw new ArgumentException("Hạn chót phải lớn hơn ngày bắt đầu.");
            }

            if (ngayBatDau.HasValue && ngayHoanThanh.HasValue && ngayHoanThanh.Value < ngayBatDau.Value)
            {
                throw new ArgumentException("Ngày hoàn thành phải lớn hơn hoặc bằng ngày bắt đầu.");
            }
        }

        private static void ApplyRiskScore(TaskListItemDto task)
        {
            var risk = CalculateRisk(
                task.HanChot,
                task.TrangThai,
                task.DoUuTien,
                task.TienDo);

            task.RiskPercent = risk;
            task.RiskLevel = GetRiskLevel(risk);
        }

        private static void ApplyRiskScore(TaskDetailDto task)
        {
            var risk = CalculateRisk(
                task.HanChot,
                task.TrangThai,
                task.DoUuTien,
                task.TienDo);

            task.RiskPercent = risk;
            task.RiskLevel = GetRiskLevel(risk);
        }

        private static bool IsDoneStatus(string? status)
        {
            return IsSameValue(status, StatusDone)
                || IsSameValue(status, StatusCanceled);
        }

        private static int CalculateRisk(
            DateOnly? deadline,
            string? status,
            string? priority,
            int? progress)
        {
            if (IsDoneStatus(status))
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

            if (IsSameValue(priority, PriorityUrgent))
            {
                score += 35;
            }
            else if (IsSameValue(priority, PriorityHigh))
            {
                score += 25;
            }
            else if (IsSameValue(priority, PriorityMedium))
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

        private static string GetRiskLevel(int risk)
        {
            if (risk >= 70)
            {
                return "High";
            }

            if (risk >= 31)
            {
                return "Medium";
            }

            return "Low";
        }

        private static bool IsSameValue(string? value, string expected)
        {
            return string.Equals(
                value?.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase);
        }
    }

}
