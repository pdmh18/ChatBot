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
                excludedTaskId,
                cancellationToken);

            var qualityScore = await _context.NangLucThanhViens
                .AsNoTracking()
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.DiemChatLuongTrungBinh)
                .FirstOrDefaultAsync(cancellationToken);

            user.KhoiLuongHienTai = workload.TongGioUocTinh;
            user.DiemChatLuongTrungBinh = qualityScore ?? 5m;
            user.PhanTramTai = ClampRatio(workload.ApLucTai);

            return user;
        }
        private static decimal ClampRatio(decimal value)
        {
            if (value < 0m)
            {
                return 0m;
            }

            if (value > 1m)
            {
                return 1m;
            }

            return value;
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
            int? excludedTaskId,
            CancellationToken cancellationToken)
        {
            if (excludedTaskId.HasValue)
            {
                return await GetWorkloadExcludingTaskAsync(
                    userId,
                    projectId,
                    sprintId,
                    excludedTaskId.Value,
                    cancellationToken);
            }

            var result = await _context.Database
                .SqlQuery<AiWorkloadRow>($"""
            SELECT TOP (1)
                CAST(ISNULL(w.SoTask, 0) AS BIGINT) AS SoTask,
                CAST(ISNULL(w.TongGioUocTinh, 0) AS DECIMAL(18, 2)) AS TongGioUocTinh,
                CAST(ISNULL(w.ApLucTai, 0) AS DECIMAL(18, 4)) AS ApLucTai
            FROM dbo.v_Workload_NhanSu_Sprint_NangCao w
            WHERE
                w.MaNguoiDung = {userId}
                AND w.MaDuAnContext = {projectId}
                AND
                (
                    ({sprintId} IS NOT NULL AND w.MaSprintContext = {sprintId})
                    OR
                    ({sprintId} IS NULL AND w.MaDuAnContext = {projectId})
                )
            ORDER BY
                w.PhanTramTaiTongHop DESC,
                w.TongGioUocTinh DESC
            """)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? new AiWorkloadRow();
        }

        private async Task<AiWorkloadRow> GetWorkloadExcludingTaskAsync(
            int userId,
            int projectId,
            int? sprintId,
            int excludedTaskId,
            CancellationToken cancellationToken)
        {
            var result = await _context.Database
                .SqlQuery<AiWorkloadRow>($"""
            WITH TargetContext AS
            (
                SELECT TOP (1)
                    w.MaSprintContext,
                    w.MaDuAnContext,
                    w.NgayBatDauContext,
                    w.NgayKetThucContext
                FROM dbo.v_Workload_NhanSu_Sprint_NangCao w
                WHERE
                    w.MaNguoiDung = {userId}
                    AND w.MaDuAnContext = {projectId}
                    AND
                    (
                        ({sprintId} IS NOT NULL AND w.MaSprintContext = {sprintId})
                        OR
                        ({sprintId} IS NULL AND w.MaDuAnContext = {projectId})
                    )
                ORDER BY
                    w.PhanTramTaiTongHop DESC,
                    w.TongGioUocTinh DESC
            ),
            WorkItems AS
            (
                SELECT
                    cv.MaCongViec,
                    cv.MaDuAn,
                    cv.SoGioUocTinh,
                    cv.DoUuTien,
                    cv.HanChot,
                    cv.TienDo,
                    cv.TrangThai,
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM dbo.PhuThuocCongViec p
                            WHERE p.MaCongViecSau = cv.MaCongViec
                        )
                        THEN 1
                        ELSE 0
                    END AS CoPhuThuocTruoc,
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM dbo.PhuThuocCongViec p
                            WHERE p.MaCongViecTruoc = cv.MaCongViec
                        )
                        THEN 1
                        ELSE 0
                    END AS DangChanCongViecSau
                FROM TargetContext tc
                JOIN dbo.Sprint sw
                    ON sw.NgayBatDau <= tc.NgayKetThucContext
                   AND sw.NgayKetThuc >= tc.NgayBatDauContext
                JOIN dbo.CongViec cv
                    ON cv.MaSprint = sw.MaSprint
                WHERE cv.MaNguoiPhuTrach = {userId}
                  AND cv.MaCongViec <> {excludedTaskId}
                  AND cv.MaSprint IS NOT NULL
                  AND ISNULL(cv.TrangThai, '') NOT IN (N'Hoan thanh', N'Da huy')
            ),
            Raw AS
            (
                SELECT
                    CAST(COUNT_BIG(*) AS BIGINT) AS SoTask,
                    COUNT(DISTINCT MaDuAn) AS SoDuAn,
                    CAST(ISNULL(SUM(ISNULL(SoGioUocTinh, 0)), 0) AS DECIMAL(18, 2)) AS TongGioUocTinh,
                    CAST(
                        ISNULL(
                            SUM(
                                ISNULL(SoGioUocTinh, 0) *
                                CASE
                                    WHEN DoUuTien = N'Khan cap' THEN CAST(1.50 AS DECIMAL(5, 2))
                                    WHEN DoUuTien = N'Cao' THEN CAST(1.20 AS DECIMAL(5, 2))
                                    WHEN DoUuTien = N'Trung binh' THEN CAST(1.00 AS DECIMAL(5, 2))
                                    WHEN DoUuTien = N'Thap' THEN CAST(0.80 AS DECIMAL(5, 2))
                                    ELSE CAST(1.00 AS DECIMAL(5, 2))
                                END
                            ),
                            0
                        )
                        AS DECIMAL(18, 2)
                    ) AS TongGioQuyDoi,
                    SUM(CASE WHEN DoUuTien = N'Cao' THEN 1 ELSE 0 END) AS SoTaskCao,
                    SUM(CASE WHEN DoUuTien = N'Khan cap' THEN 1 ELSE 0 END) AS SoTaskKhanCap,
                    SUM(
                        CASE
                            WHEN HanChot IS NOT NULL
                             AND HanChot < CAST(GETDATE() AS DATE)
                            THEN 1
                            ELSE 0
                        END
                    ) AS SoTaskQuaHan,
                    SUM(
                        CASE
                            WHEN HanChot IS NOT NULL
                             AND HanChot >= CAST(GETDATE() AS DATE)
                             AND HanChot <= DATEADD(DAY, 3, CAST(GETDATE() AS DATE))
                            THEN 1
                            ELSE 0
                        END
                    ) AS SoTaskGanHan,
                    SUM(CASE WHEN ISNULL(TienDo, 0) < 30 THEN 1 ELSE 0 END) AS SoTaskTienDoThap,
                    SUM(CASE WHEN TrangThai = N'Bi chan' THEN 1 ELSE 0 END) AS SoTaskBiChan,
                    SUM(CASE WHEN CoPhuThuocTruoc = 1 THEN 1 ELSE 0 END) AS SoTaskCoPhuThuocTruoc,
                    SUM(CASE WHEN DangChanCongViecSau = 1 THEN 1 ELSE 0 END) AS SoTaskChanCongViecSau
                FROM WorkItems
            ),
            Capacity AS
            (
                SELECT
                    CAST(ISNULL(NULLIF(nd.KhoiLuongToiDa, 0), 40) AS DECIMAL(18, 2)) AS KhoiLuongToiDa
                FROM dbo.NguoiDung nd
                WHERE nd.MaNguoiDung = {userId}
            ),
            Components AS
            (
                SELECT
                    Raw.SoTask,
                    Raw.SoDuAn,
                    Raw.TongGioUocTinh,
                    CAST(
                        CASE
                            WHEN Capacity.KhoiLuongToiDa <= 0 THEN 0
                            WHEN Raw.TongGioQuyDoi / Capacity.KhoiLuongToiDa > 1 THEN 1
                            ELSE Raw.TongGioQuyDoi / Capacity.KhoiLuongToiDa
                        END
                        AS DECIMAL(18, 4)
                    ) AS GioLoad,
                    CAST(
                        CASE
                            WHEN CAST(Raw.SoTask AS DECIMAL(18, 4)) / 8.0 > 1 THEN 1
                            ELSE CAST(Raw.SoTask AS DECIMAL(18, 4)) / 8.0
                        END
                        AS DECIMAL(18, 4)
                    ) AS TaskLoad,
                    CAST(
                        CASE
                            WHEN CAST(Raw.SoDuAn AS DECIMAL(18, 4)) / 3.0 > 1 THEN 1
                            ELSE CAST(Raw.SoDuAn AS DECIMAL(18, 4)) / 3.0
                        END
                        AS DECIMAL(18, 4)
                    ) AS ProjectLoad,
                    CAST(
                        CASE
                            WHEN Raw.SoTask <= 0 THEN 0
                            WHEN
                                (
                                    (CAST(Raw.SoTaskKhanCap AS DECIMAL(18, 4)) * 1.00)
                                    + (CAST(Raw.SoTaskCao AS DECIMAL(18, 4)) * 0.70)
                                ) / Raw.SoTask > 1 THEN 1
                            ELSE
                                (
                                    (CAST(Raw.SoTaskKhanCap AS DECIMAL(18, 4)) * 1.00)
                                    + (CAST(Raw.SoTaskCao AS DECIMAL(18, 4)) * 0.70)
                                ) / Raw.SoTask
                        END
                        AS DECIMAL(18, 4)
                    ) AS PriorityLoad,
                    CAST(
                        CASE
                            WHEN Raw.SoTask <= 0 THEN 0
                            WHEN
                                (
                                    (CAST(Raw.SoTaskQuaHan AS DECIMAL(18, 4)) * 1.00)
                                    + (CAST(Raw.SoTaskGanHan AS DECIMAL(18, 4)) * 0.70)
                                ) / Raw.SoTask > 1 THEN 1
                            ELSE
                                (
                                    (CAST(Raw.SoTaskQuaHan AS DECIMAL(18, 4)) * 1.00)
                                    + (CAST(Raw.SoTaskGanHan AS DECIMAL(18, 4)) * 0.70)
                                ) / Raw.SoTask
                        END
                        AS DECIMAL(18, 4)
                    ) AS DeadlineLoad,
                    CAST(
                        CASE
                            WHEN Raw.SoTask <= 0 THEN 0
                            WHEN CAST(Raw.SoTaskTienDoThap AS DECIMAL(18, 4)) / Raw.SoTask > 1 THEN 1
                            ELSE CAST(Raw.SoTaskTienDoThap AS DECIMAL(18, 4)) / Raw.SoTask
                        END
                        AS DECIMAL(18, 4)
                    ) AS ProgressLoad,
                    CAST(
                        CASE
                            WHEN Raw.SoTask <= 0 THEN 0
                            WHEN
                                (
                                    (CAST(Raw.SoTaskBiChan AS DECIMAL(18, 4)) * 1.00)
                                    + (CAST(Raw.SoTaskChanCongViecSau AS DECIMAL(18, 4)) * 0.50)
                                    + (CAST(Raw.SoTaskCoPhuThuocTruoc AS DECIMAL(18, 4)) * 0.30)
                                ) / Raw.SoTask > 1 THEN 1
                            ELSE
                                (
                                    (CAST(Raw.SoTaskBiChan AS DECIMAL(18, 4)) * 1.00)
                                    + (CAST(Raw.SoTaskChanCongViecSau AS DECIMAL(18, 4)) * 0.50)
                                    + (CAST(Raw.SoTaskCoPhuThuocTruoc AS DECIMAL(18, 4)) * 0.30)
                                ) / Raw.SoTask
                        END
                        AS DECIMAL(18, 4)
                    ) AS BlockedLoad
                FROM Raw
                CROSS JOIN Capacity
            ),
            FinalScore AS
            (
                SELECT
                    SoTask,
                    TongGioUocTinh,
                    CAST(
                        CASE
                            WHEN
                                (
                                    0.35 * GioLoad
                                    + 0.12 * TaskLoad
                                    + 0.08 * ProjectLoad
                                    + 0.15 * PriorityLoad
                                    + 0.15 * DeadlineLoad
                                    + 0.10 * ProgressLoad
                                    + 0.05 * BlockedLoad
                                ) > 1 THEN 1
                            ELSE
                                (
                                    0.35 * GioLoad
                                    + 0.12 * TaskLoad
                                    + 0.08 * ProjectLoad
                                    + 0.15 * PriorityLoad
                                    + 0.15 * DeadlineLoad
                                    + 0.10 * ProgressLoad
                                    + 0.05 * BlockedLoad
                                )
                        END
                        AS DECIMAL(18, 4)
                    ) AS ApLucTai
                FROM Components
            )
            SELECT
                CAST(ISNULL(SoTask, 0) AS BIGINT) AS SoTask,
                CAST(ISNULL(TongGioUocTinh, 0) AS DECIMAL(18, 2)) AS TongGioUocTinh,
                CAST(ISNULL(ApLucTai, 0) AS DECIMAL(18, 4)) AS ApLucTai
            FROM FinalScore
            """)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? new AiWorkloadRow();
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
    $"Tong gio uoc tinh: {user.KhoiLuongHienTai:0.##}/{user.KhoiLuongToiDa:0.##}h. Ap luc tai tong hop: {user.PhanTramTai:P0}.";

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

            public decimal ApLucTai { get; set; }
        }
    }
}
