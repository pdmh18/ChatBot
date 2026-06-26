using Application.Common.DTOs.Ai;
using Application.Features.Ai;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class AiPredictionRepository : IAiPredictionRepository
    {
        private const string StaffMatchingModelName = "Random Forest Staff Matching";

        private const string StatusDone = "Hoan thanh";
        private const string StatusCanceled = "Da huy";

        private readonly QuanLyDuAnAiContext _context;

        public AiPredictionRepository(QuanLyDuAnAiContext context)
        {
            _context = context;
        }

        public async Task<AiTaskDataDto?> GetTaskDataAsync(
            int taskId,
            CancellationToken cancellationToken = default)
        {
            return await _context.CongViecs
                .AsNoTracking()
                .Where(x => x.MaCongViec == taskId)
                .Select(x => new AiTaskDataDto
                {
                    MaCongViec = x.MaCongViec,
                    MaDuAn = x.MaDuAn,
                    MaSprint = x.MaSprint,
                    TenCongViec = x.TenCongViec,
                    SoGioUocTinh = x.SoGioUocTinh ?? 0m,
                    DoUuTien = x.DoUuTien ?? "Trung binh",
                    MaNguoiPhuTrach = x.MaNguoiPhuTrach
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AiUserDataDto?> GetUserDataAsync(
            int userId,
            int projectId,
            int? sprintId,
            int? excludedTaskId,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.NguoiDungs
                .AsNoTracking()
                .Where(x => x.MaNguoiDung == userId && x.DangHoatDong != false)
                .Select(x => new AiUserDataDto
                {
                    MaNguoiDung = x.MaNguoiDung,
                    HoTen = x.HoTen,
                    SoNamKinhNghiem = x.SoNamKinhNghiem ?? 0,

                    // Không dùng NguoiDung.KhoiLuongHienTai.
                    // Workload sẽ được lấy từ view workload theo sprint.
                    KhoiLuongHienTai = 0m,

                    KhoiLuongToiDa = x.KhoiLuongToiDa ?? 40m,
                    DiemChatLuongTrungBinh = 5m
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return null;
            }

            var workload = await GetWorkloadFromViewAsync(
                userId,
                projectId,
                sprintId,
                cancellationToken);

            var currentWorkload = workload.TongGioUocTinh;

            if (excludedTaskId.HasValue)
            {
                var excludedHours = await GetExcludedTaskHoursAsync(
                    excludedTaskId.Value,
                    userId,
                    projectId,
                    sprintId,
                    cancellationToken);

                currentWorkload -= excludedHours;

                if (currentWorkload < 0m)
                {
                    currentWorkload = 0m;
                }
            }

            var qualityScore = await _context.NangLucThanhViens
                .AsNoTracking()
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.DiemChatLuongTrungBinh)
                .FirstOrDefaultAsync(cancellationToken);

            user.KhoiLuongHienTai = currentWorkload;
            user.DiemChatLuongTrungBinh = qualityScore ?? 5m;
            user.PhanTramTai = CalculateWorkloadRatio(
                user.KhoiLuongHienTai,
                user.KhoiLuongToiDa);

            return user;
        }

        public Task<int> CountPreviousDependenciesAsync(
            int taskId,
            CancellationToken cancellationToken = default)
        {
            return _context.PhuThuocCongViecs
                .AsNoTracking()
                .CountAsync(x => x.MaCongViecSau == taskId, cancellationToken);
        }

        public async Task<IReadOnlyList<int>> GetProjectMemberUserIdsAsync(
            int projectId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ThanhVienDuAns
                .AsNoTracking()
                .Where(x =>
                    x.MaDuAn == projectId &&
                    x.NgayRoiDuAn == null &&
                    x.MaNguoiDungNavigation.DangHoatDong != false)
                .Select(x => x.MaNguoiDung)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetOrCreateRiskTypeAsync(
            string riskTypeName,
            string? description,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.LoaiRuiRos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenLoaiRuiRo == riskTypeName, cancellationToken);

            if (existing != null)
            {
                return existing.MaLoaiRuiRo;
            }

            var entity = new LoaiRuiRo
            {
                TenLoaiRuiRo = riskTypeName,
                MoTa = description
            };

            _context.LoaiRuiRos.Add(entity);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return entity.MaLoaiRuiRo;
            }
            catch (DbUpdateException)
            {
                _context.Entry(entity).State = EntityState.Detached;

                var existingAfterRace = await _context.LoaiRuiRos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenLoaiRuiRo == riskTypeName, cancellationToken);

                if (existingAfterRace != null)
                {
                    return existingAfterRace.MaLoaiRuiRo;
                }

                throw;
            }
        }

        public async Task<RiskPredictionResultDto> SaveRiskPredictionAsync(
            AiTaskDataDto task,
            int riskTypeId,
            TaskRiskAiResponse aiResult,
            CancellationToken cancellationToken = default)
        {
            var entity = new DuBaoRuiRoAI
            {
                MaDuAn = task.MaDuAn,
                MaCongViec = task.MaCongViec,
                MaLoaiRuiRo = riskTypeId,
                TenMoHinh = "XGBoost Risk Model",
                PhienBanMoHinh = "1.0",
                XacSuatRuiRo = Round2(aiResult.XacSuatTreHan),
                MucDoRuiRo = NormalizeRiskLevel(aiResult.MucDoRuiRo),
                TacDongDuBao = BuildRiskImpact(aiResult),
                KhuyenNghi = BuildRiskRecommendation(aiResult),
                NgayDuBao = DateTime.UtcNow
            };

            _context.DuBaoRuiRoAIs.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return new RiskPredictionResultDto
            {
                MaDuBao = entity.MaDuBao,
                MaDuAn = entity.MaDuAn,
                MaCongViec = entity.MaCongViec,
                MaLoaiRuiRo = entity.MaLoaiRuiRo,
                TenMoHinh = entity.TenMoHinh,
                XacSuatRuiRo = entity.XacSuatRuiRo,
                DuBaoTreHan = aiResult.DuBaoTreHan,
                MucDoRuiRo = entity.MucDoRuiRo,
                TacDongDuBao = entity.TacDongDuBao,
                KhuyenNghi = entity.KhuyenNghi,
                NgayDuBao = entity.NgayDuBao
            };
        }

        public async Task<StaffMatchResultDto> SaveStaffMatchAsync(
            AiTaskDataDto task,
            AiUserDataDto user,
            StaffMatchAiResponse aiResult,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            var oldSuggestions = await _context.DeXuatGiaoViecAIs
                .Where(x =>
                    x.MaCongViec == task.MaCongViec &&
                    x.MaNguoiDuocDeXuat == user.MaNguoiDung &&
                    x.TenMoHinh == StaffMatchingModelName)
                .ToListAsync(cancellationToken);

            _context.DeXuatGiaoViecAIs.RemoveRange(oldSuggestions);

            var entity = BuildStaffMatchEntity(
                task,
                user,
                aiResult,
                DateTime.UtcNow);

            _context.DeXuatGiaoViecAIs.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToStaffMatchResultDto(entity, user.HoTen);
        }

        public async Task<IReadOnlyList<StaffMatchResultDto>> ReplaceStaffSuggestionsAsync(
            AiTaskDataDto task,
            IReadOnlyList<StaffMatchSaveItemDto> suggestions,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            var oldSuggestions = await _context.DeXuatGiaoViecAIs
                .Where(x =>
                    x.MaCongViec == task.MaCongViec &&
                    x.TenMoHinh == StaffMatchingModelName)
                .ToListAsync(cancellationToken);

            _context.DeXuatGiaoViecAIs.RemoveRange(oldSuggestions);

            var now = DateTime.UtcNow;

            var newSuggestions = suggestions
                .Select(x => new
                {
                    x.User,
                    Entity = BuildStaffMatchEntity(
                        task,
                        x.User,
                        x.AiResult,
                        now)
                })
                .ToList();

            _context.DeXuatGiaoViecAIs.AddRange(newSuggestions.Select(x => x.Entity));

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return newSuggestions
                .Select(x => ToStaffMatchResultDto(x.Entity, x.User.HoTen))
                .OrderByDescending(x => x.DiemPhuHop)
                .ToList();
        }

        public async Task<IReadOnlyList<BottleneckResultDto>> SaveBottleneckResultsAsync(
            IReadOnlyList<BottleneckAiResponse> aiResults,
            CancellationToken cancellationToken = default)
        {
            if (aiResults.Count == 0)
            {
                return Array.Empty<BottleneckResultDto>();
            }

            var taskIds = aiResults
                .Select(x => x.MaCongViec)
                .Distinct()
                .ToList();

            var tasksById = await _context.CongViecs
                .AsNoTracking()
                .Where(x => taskIds.Contains(x.MaCongViec))
                .Select(x => new
                {
                    x.MaCongViec,
                    x.MaDuAn
                })
                .ToDictionaryAsync(x => x.MaCongViec, cancellationToken);

            var itemsToSave = new List<(DiemNghenAI Entity, BottleneckAiResponse AiResult)>();
            var now = DateTime.UtcNow;

            foreach (var result in aiResults)
            {
                if (!tasksById.TryGetValue(result.MaCongViec, out var task))
                {
                    continue;
                }

                var entity = new DiemNghenAI
                {
                    MaDuAn = task.MaDuAn,
                    MaCongViec = task.MaCongViec,
                    KhuVucPhatHien = "Cong viec phu thuoc",
                    NguyenNhan = BuildBottleneckReason(result),
                    MucDoNghiemTrong = GetBottleneckSeverity(result.BottleneckScore),
                    SoNgayTreDuBao = 0,
                    KhuyenNghiAI = BuildBottleneckRecommendation(result),
                    NgayPhatHien = now
                };

                itemsToSave.Add((entity, result));
            }

            if (itemsToSave.Count == 0)
            {
                return Array.Empty<BottleneckResultDto>();
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            _context.DiemNghenAIs.AddRange(itemsToSave.Select(x => x.Entity));

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return itemsToSave
                .Select(x => new BottleneckResultDto
                {
                    MaDiemNghen = x.Entity.MaDiemNghen,
                    MaDuAn = x.Entity.MaDuAn,
                    MaCongViec = x.Entity.MaCongViec,

                    SoTaskBiAnhHuongPhiaSau = x.AiResult.SoTaskBiAnhHuongPhiaSau,
                    BottleneckScore = x.AiResult.BottleneckScore,

                    KhuVucPhatHien = x.Entity.KhuVucPhatHien,
                    NguyenNhan = x.Entity.NguyenNhan,
                    MucDoNghiemTrong = x.Entity.MucDoNghiemTrong,
                    SoNgayTreDuBao = x.Entity.SoNgayTreDuBao,
                    KhuyenNghiAI = x.Entity.KhuyenNghiAI,
                    NgayPhatHien = x.Entity.NgayPhatHien
                })
                .ToList();
        }

        private async Task<AiWorkloadRow> GetWorkloadFromViewAsync(
            int userId,
            int projectId,
            int? sprintId,
            CancellationToken cancellationToken)
        {
            var result = await _context.Database
                .SqlQuery<AiWorkloadRow>($"""
                    SELECT
                        CAST(ISNULL(w.SoTask, 0) AS BIGINT) AS SoTask,
                        CAST(ISNULL(w.TongGioUocTinh, 0) AS DECIMAL(18, 2)) AS TongGioUocTinh
                    FROM (SELECT 1 AS Dummy) d
                    LEFT JOIN dbo.v_Workload_NhanSu_Sprint w
                        ON w.MaDuAn = {projectId}
                       AND w.MaNguoiDung = {userId}
                       AND (
                            ({sprintId} IS NULL AND w.MaSprint IS NULL)
                            OR
                            ({sprintId} IS NOT NULL AND w.MaSprint = {sprintId})
                       )
                    """)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? new AiWorkloadRow();
        }

        private async Task<decimal> GetExcludedTaskHoursAsync(
            int taskId,
            int userId,
            int projectId,
            int? sprintId,
            CancellationToken cancellationToken)
        {
            var query = _context.CongViecs
                .AsNoTracking()
                .Where(x =>
                    x.MaCongViec == taskId &&
                    x.MaNguoiPhuTrach == userId &&
                    x.MaDuAn == projectId &&
                    (
                        x.TrangThai == null ||
                        (
                            x.TrangThai != StatusDone &&
                            x.TrangThai != StatusCanceled
                        )
                    ));

            if (sprintId.HasValue)
            {
                query = query.Where(x => x.MaSprint == sprintId.Value);
            }
            else
            {
                query = query.Where(x => x.MaSprint == null);
            }

            return await query
                .Select(x => x.SoGioUocTinh ?? 0m)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static DeXuatGiaoViecAI BuildStaffMatchEntity(
            AiTaskDataDto task,
            AiUserDataDto user,
            StaffMatchAiResponse aiResult,
            DateTime createdAt)
        {
            return new DeXuatGiaoViecAI
            {
                MaCongViec = task.MaCongViec,
                MaNguoiDuocDeXuat = user.MaNguoiDung,
                TenMoHinh = StaffMatchingModelName,
                DiemPhuHop = Round2(aiResult.XacSuatHieuQua),
                DiemKyNang = Round2(user.DiemChatLuongTrungBinh),

                // DiemKhoiLuong = mức độ còn rảnh.
                // Ví dụ tải 80% thì còn rảnh 20%.
                DiemKhoiLuong = Round2(1m - user.PhanTramTai),

                DiemKinhNghiem = Round2((decimal)user.SoNamKinhNghiem),
                LyDo = BuildStaffReason(aiResult, user),
                DaChapNhan = false,
                NgayTao = createdAt
            };
        }

        private static StaffMatchResultDto ToStaffMatchResultDto(
            DeXuatGiaoViecAI entity,
            string hoTenNguoiDuocDeXuat)
        {
            return new StaffMatchResultDto
            {
                MaDeXuat = entity.MaDeXuat,
                MaCongViec = entity.MaCongViec,
                MaNguoiDuocDeXuat = entity.MaNguoiDuocDeXuat,
                HoTenNguoiDuocDeXuat = hoTenNguoiDuocDeXuat,
                TenMoHinh = entity.TenMoHinh,
                DiemPhuHop = entity.DiemPhuHop,
                DiemKyNang = entity.DiemKyNang,
                DiemKhoiLuong = entity.DiemKhoiLuong,
                DiemKinhNghiem = entity.DiemKinhNghiem,
                LyDo = entity.LyDo,
                DaChapNhan = entity.DaChapNhan,
                NgayTao = entity.NgayTao
            };
        }

        private static string BuildRiskImpact(TaskRiskAiResponse aiResult)
        {
            if (!string.IsNullOrWhiteSpace(aiResult.NguyenNhan))
            {
                return aiResult.NguyenNhan.Trim();
            }

            return aiResult.DuBaoTreHan
                ? "Task co nguy co tre han"
                : "Nguy co tre han thap";
        }

        private static string BuildRiskRecommendation(TaskRiskAiResponse aiResult)
        {
            var normalizedRiskLevel = NormalizeRiskLevel(aiResult.MucDoRuiRo);

            if (aiResult.DuBaoTreHan || normalizedRiskLevel == "Cao")
            {
                return "Can xem lai han chot, khoi luong va nguoi phu trach.";
            }

            if (normalizedRiskLevel == "Trung binh")
            {
                return "Nen theo doi sat tien do va kiem tra tai hien tai cua nhan su.";
            }

            return "Tiep tuc theo doi tien do task.";
        }

        private static string BuildStaffReason(
            StaffMatchAiResponse aiResult,
            AiUserDataDto user)
        {
            var decision = aiResult.DeXuatGiaoViec
                ? "AI de xuat giao viec"
                : "AI khong de xuat giao viec";

            var matchLevel = string.IsNullOrWhiteSpace(aiResult.MucDoPhuHop)
                ? "Khong xac dinh"
                : aiResult.MucDoPhuHop.Trim();

            var workloadText =
                $"Tai hien tai trong sprint: {user.KhoiLuongHienTai:0.##}/{user.KhoiLuongToiDa:0.##}h ({user.PhanTramTai:P0}).";

            if (!string.IsNullOrWhiteSpace(aiResult.NguyenNhan))
            {
                return $"{decision}. Muc do phu hop: {matchLevel}. {workloadText} Ly do: {aiResult.NguyenNhan.Trim()}";
            }

            return $"{decision}. Muc do phu hop: {matchLevel}. {workloadText}";
        }

        private static string BuildBottleneckReason(BottleneckAiResponse result)
        {
            if (result.SoTaskBiAnhHuongPhiaSau > 0)
            {
                return $"Task này đang chặn {result.SoTaskBiAnhHuongPhiaSau} task phía sau. Nếu task này trễ, các task phụ thuộc phía sau có nguy cơ bị kéo trễ. Bottleneck score = {result.BottleneckScore:0.0000}.";
            }

            return $"Task này có bottleneck score = {result.BottleneckScore:0.0000}, mức ảnh hưởng hiện tại thấp.";
        }

        private static string BuildBottleneckRecommendation(BottleneckAiResponse result)
        {
            if (result.BottleneckScore >= 0.7)
            {
                return "Can uu tien xu ly task nay vi co nguy co gay tac nghen cho nhieu cong viec phia sau.";
            }

            if (result.BottleneckScore >= 0.4)
            {
                return "Nen theo doi task nay va dam bao cac phu thuoc duoc xu ly dung han.";
            }

            return "Tiep tuc theo doi, hien tai muc do diem nghen thap.";
        }

        private static string GetBottleneckSeverity(double score)
        {
            if (score >= 0.7)
            {
                return "Cao";
            }

            if (score >= 0.4)
            {
                return "Trung binh";
            }

            return "Thap";
        }

        private static decimal CalculateWorkloadRatio(decimal current, decimal max)
        {
            if (max <= 0m)
            {
                return 0m;
            }

            var ratio = current / max;

            if (ratio < 0m)
            {
                return 0m;
            }

            if (ratio > 1m)
            {
                return 1m;
            }

            return ratio;
        }

        private static decimal Round2(double value)
        {
            return Math.Round((decimal)value, 2);
        }

        private static decimal Round2(decimal value)
        {
            return Math.Round(value, 2);
        }

        private static string NormalizeRiskLevel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Khong xac dinh";
            }

            var normalized = value.Trim();

            if (string.Equals(normalized, "Thấp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Thap", StringComparison.OrdinalIgnoreCase))
            {
                return "Thap";
            }

            if (string.Equals(normalized, "Trung bình", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Trung binh", StringComparison.OrdinalIgnoreCase))
            {
                return "Trung binh";
            }

            if (string.Equals(normalized, "Cao", StringComparison.OrdinalIgnoreCase))
            {
                return "Cao";
            }

            return normalized;
        }

        private sealed class AiWorkloadRow
        {
            public long SoTask { get; set; }
            public decimal TongGioUocTinh { get; set; }
        }
    }
}