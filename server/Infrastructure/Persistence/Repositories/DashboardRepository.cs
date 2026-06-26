using Application.Features.Dashboard;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Application.Common.DTOs.Dashboard.DashboardDtos;

namespace Infrastructure.Persistence.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly QuanLyDuAnAiContext _context;

        public DashboardRepository(QuanLyDuAnAiContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<DashboardTaskAlertDto>> GetTasksAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.CongViecs
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(x => x.MaDuAn == projectId.Value);
            }

            if (sprintId.HasValue)
            {
                query = query.Where(x => x.MaSprint == sprintId.Value);
            }

            return await query
                .Select(x => new DashboardTaskAlertDto
                {
                    MaCongViec = x.MaCongViec,
                    MaCongViecCode = x.MaCongViecCode,
                    TenCongViec = x.TenCongViec,
                    MaDuAn = x.MaDuAn,
                    MaSprint = x.MaSprint,

                    MaNguoiPhuTrach = x.MaNguoiPhuTrach,
                    NguoiPhuTrach = x.MaNguoiPhuTrachNavigation != null
                        ? x.MaNguoiPhuTrachNavigation.HoTen
                        : null,

                    TrangThai = x.TrangThai,
                    DoUuTien = x.DoUuTien,
                    HanChot = x.HanChot,
                    TienDo = x.TienDo,
                    SoGioUocTinh = x.SoGioUocTinh
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DashboardWorkloadDto>> GetWorkloadAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.Database
                .SqlQuery<DashboardWorkloadRow>($"""
                    SELECT
                        CAST(w.MaNguoiDung AS INT) AS MaNguoiDung,
                        CAST(COALESCE(MAX(nd.HoTen), N'Khong ro') AS NVARCHAR(MAX)) AS HoTen,
                        CAST(SUM(w.SoTask) AS INT) AS SoTask,
                        CAST(SUM(w.TongGioUocTinh) AS DECIMAL(18, 2)) AS TongGioUocTinh,
                        CAST(
                            CASE
                                WHEN ISNULL(NULLIF(MAX(nd.KhoiLuongToiDa), 0), 40) <= 0 THEN 0
                                ELSE
                                    (
                                        SUM(w.TongGioUocTinh)
                                        / ISNULL(NULLIF(MAX(nd.KhoiLuongToiDa), 0), 40)
                                    ) * 100
                            END
                            AS DECIMAL(18, 2)
                        ) AS PhanTramTai
                    FROM dbo.v_Workload_NhanSu_Sprint w
                    INNER JOIN dbo.NguoiDung nd
                        ON nd.MaNguoiDung = w.MaNguoiDung
                    WHERE
                        ({projectId} IS NULL OR w.MaDuAn = {projectId})
                        AND ({sprintId} IS NULL OR w.MaSprint = {sprintId})
                        AND ISNULL(nd.DangHoatDong, 1) <> 0
                    GROUP BY
                        w.MaNguoiDung
                    ORDER BY
                        SUM(w.TongGioUocTinh) DESC
                    """)
                .ToListAsync(cancellationToken);

            return rows
                .Select(x => new DashboardWorkloadDto
                {
                    MaNguoiDung = x.MaNguoiDung,
                    HoTen = x.HoTen,
                    SoTask = x.SoTask,
                    TongGioUocTinh = x.TongGioUocTinh,
                    PhanTramTai = x.PhanTramTai,
                    MucDoTai = string.Empty
                })
                .ToList();
        }

        public async Task<int> CountLatestBottleneckBatchAsync(
            int? projectId,
            int? sprintId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.DiemNghenAIs
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(x => x.MaDuAn == projectId.Value);
            }

            if (sprintId.HasValue)
            {
                query = query.Where(x =>
                    x.MaCongViecNavigation != null &&
                    x.MaCongViecNavigation.MaSprint == sprintId.Value);
            }

            var latestTime = await query
                .Where(x => x.NgayPhatHien != null)
                .MaxAsync(x => (DateTime?)x.NgayPhatHien, cancellationToken);

            if (!latestTime.HasValue)
            {
                return 0;
            }

            return await query.CountAsync(
                x => x.NgayPhatHien == latestTime.Value,
                cancellationToken);
        }

        private sealed class DashboardWorkloadRow
        {
            public int MaNguoiDung { get; set; }
            public string HoTen { get; set; } = string.Empty;
            public int SoTask { get; set; }
            public decimal TongGioUocTinh { get; set; }
            public decimal PhanTramTai { get; set; }
        }
    }
}