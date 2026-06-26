using Application.Features.Dashboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<int> CountLatestBottleneckBatchAsync(
            int? projectId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.DiemNghenAIs
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(x => x.MaDuAn == projectId.Value);
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
    }
}
