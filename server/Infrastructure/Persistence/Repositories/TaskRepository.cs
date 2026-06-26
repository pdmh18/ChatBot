using Application.Common.DTOs.Tasks;
using Application.Features.Tasks;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private const string StatusDone = "Hoan thanh";
        private const string StatusCanceled = "Da huy";

        private readonly QuanLyDuAnAiContext _context;

        public TaskRepository(QuanLyDuAnAiContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<TaskListItemDto>> GetListAsync(
            TaskQueryParameters query,
            CancellationToken cancellationToken = default)
        {
            var dbQuery = _context.CongViecs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                dbQuery = dbQuery.Where(x =>
                    x.TenCongViec.Contains(search) ||
                    (x.MaCongViecCode != null && x.MaCongViecCode.Contains(search)));
            }

            if (query.ProjectId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.MaDuAn == query.ProjectId.Value);
            }

            if (query.SprintId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.MaSprint == query.SprintId.Value);
            }

            if (query.AssigneeId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.MaNguoiPhuTrach == query.AssigneeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim();
                dbQuery = dbQuery.Where(x => x.TrangThai == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Priority))
            {
                var priority = query.Priority.Trim();
                dbQuery = dbQuery.Where(x => x.DoUuTien == priority);
            }

            return await dbQuery
                .OrderByDescending(x => x.NgayTao)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new TaskListItemDto
                {
                    MaCongViec = x.MaCongViec,
                    MaCongViecCode = x.MaCongViecCode,
                    TenCongViec = x.TenCongViec,

                    MaDuAn = x.MaDuAn,
                    TenDuAn = x.MaDuAnNavigation.TenDuAn,

                    MaSprint = x.MaSprint,
                    TenSprint = x.MaSprintNavigation != null
                        ? x.MaSprintNavigation.TenSprint
                        : null,

                    MaNguoiPhuTrach = x.MaNguoiPhuTrach,
                    NguoiPhuTrach = x.MaNguoiPhuTrachNavigation != null
                        ? x.MaNguoiPhuTrachNavigation.HoTen
                        : null,

                    TrangThai = x.TrangThai,
                    DoUuTien = x.DoUuTien,
                    NgayBatDau = x.NgayBatDau,
                    HanChot = x.HanChot,
                    SoGioUocTinh = x.SoGioUocTinh,
                    TienDo = x.TienDo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<TaskDetailDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.CongViecs
                .AsNoTracking()
                .Where(x => x.MaCongViec == id)
                .Select(x => new TaskDetailDto
                {
                    MaCongViec = x.MaCongViec,
                    MaCongViecCode = x.MaCongViecCode,
                    TenCongViec = x.TenCongViec,
                    MoTa = x.MoTa,

                    MaDuAn = x.MaDuAn,
                    TenDuAn = x.MaDuAnNavigation.TenDuAn,

                    MaNguoiTao = x.MaNguoiTao,
                    NguoiTao = x.MaNguoiTaoNavigation.HoTen,

                    MaNguoiPhuTrach = x.MaNguoiPhuTrach,
                    NguoiPhuTrach = x.MaNguoiPhuTrachNavigation != null
                        ? x.MaNguoiPhuTrachNavigation.HoTen
                        : null,

                    MaSprint = x.MaSprint,
                    TenSprint = x.MaSprintNavigation != null
                        ? x.MaSprintNavigation.TenSprint
                        : null,

                    DoUuTien = x.DoUuTien,
                    TrangThai = x.TrangThai,
                    NgayBatDau = x.NgayBatDau,
                    HanChot = x.HanChot,
                    NgayHoanThanh = x.NgayHoanThanh,
                    SoGioUocTinh = x.SoGioUocTinh,
                    SoGioThucTe = x.SoGioThucTe,
                    TienDo = x.TienDo,
                    NgayTao = x.NgayTao,
                    NgayCapNhat = x.NgayCapNhat
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CreateAsync(
            CreateTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var entity = new CongViec
            {
                MaDuAn = request.MaDuAn,
                MaSprint = request.MaSprint,
                MaCongViecCode = request.MaCongViecCode,
                TenCongViec = request.TenCongViec,
                MoTa = request.MoTa,
                MaNguoiTao = request.MaNguoiTao,
                MaNguoiPhuTrach = request.MaNguoiPhuTrach,
                DoUuTien = request.DoUuTien,
                TrangThai = request.TrangThai,
                NgayBatDau = request.NgayBatDau,
                HanChot = request.HanChot,
                SoGioUocTinh = request.SoGioUocTinh,
                TienDo = request.TienDo,
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow
            };

            _context.CongViecs.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entity.MaCongViec;
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.CongViecs
                .FirstOrDefaultAsync(x => x.MaCongViec == id, cancellationToken);

            if (entity == null)
            {
                return false;
            }

            entity.MaDuAn = request.MaDuAn;
            entity.MaSprint = request.MaSprint;
            entity.MaCongViecCode = request.MaCongViecCode;
            entity.TenCongViec = request.TenCongViec;
            entity.MoTa = request.MoTa;
            entity.MaNguoiPhuTrach = request.MaNguoiPhuTrach;
            entity.DoUuTien = request.DoUuTien;
            entity.TrangThai = request.TrangThai;
            entity.NgayBatDau = request.NgayBatDau;
            entity.HanChot = request.HanChot;
            entity.NgayHoanThanh = request.NgayHoanThanh;
            entity.SoGioUocTinh = request.SoGioUocTinh;
            entity.SoGioThucTe = request.SoGioThucTe;
            entity.TienDo = request.TienDo;
            entity.NgayCapNhat = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.CongViecs
                .FirstOrDefaultAsync(x => x.MaCongViec == id, cancellationToken);

            if (entity == null)
            {
                return false;
            }

            _context.CongViecs.Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> UpdateStatusAsync(
            int id,
            UpdateTaskStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.CongViecs
                .FirstOrDefaultAsync(x => x.MaCongViec == id, cancellationToken);

            if (entity == null)
            {
                return false;
            }

            entity.TrangThai = request.TrangThai;
            entity.NgayCapNhat = DateTime.UtcNow;

            if (request.TienDo.HasValue)
            {
                entity.TienDo = request.TienDo.Value;
            }

            if (IsDoneStatus(request.TrangThai))
            {
                entity.TienDo = 100;
                entity.NgayHoanThanh ??= DateOnly.FromDateTime(DateTime.Today);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        private async Task AdjustUserWorkloadAsync(
    int userId,
    decimal delta,
    CancellationToken cancellationToken)
        {
            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);

            if (user == null)
            {
                return;
            }

            var currentWorkload = user.KhoiLuongHienTai ?? 0m;
            var newWorkload = currentWorkload + delta;

            if (newWorkload < 0m)
            {
                newWorkload = 0m;
            }

            user.KhoiLuongHienTai = newWorkload;
        }

        private static bool IsActiveForWorkload(string? status)
        {
            return !string.Equals(
                       status?.Trim(),
                       StatusDone,
                       StringComparison.OrdinalIgnoreCase)
                   &&
                   !string.Equals(
                       status?.Trim(),
                       StatusCanceled,
                       StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> AssignAsync(
    int id,
    AssignTaskRequest request,
    CancellationToken cancellationToken = default)
        {
            var entity = await _context.CongViecs
                .FirstOrDefaultAsync(x => x.MaCongViec == id, cancellationToken);

            if (entity == null)
            {
                return false;
            }

            var oldAssigneeId = entity.MaNguoiPhuTrach;
            var newAssigneeId = request.MaNguoiPhuTrach;

            if (oldAssigneeId == newAssigneeId)
            {
                entity.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            var estimatedHours = entity.SoGioUocTinh ?? 0m;
            var shouldUpdateWorkload = IsActiveForWorkload(entity.TrangThai) && estimatedHours > 0m;

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            if (shouldUpdateWorkload)
            {
                if (oldAssigneeId.HasValue)
                {
                    await AdjustUserWorkloadAsync(
                        oldAssigneeId.Value,
                        -estimatedHours,
                        cancellationToken);
                }

                if (newAssigneeId.HasValue)
                {
                    await AdjustUserWorkloadAsync(
                        newAssigneeId.Value,
                        estimatedHours,
                        cancellationToken);
                }
            }

            entity.MaNguoiPhuTrach = newAssigneeId;
            entity.NgayCapNhat = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }

        public Task<bool> ProjectExistsAsync(
            int projectId,
            CancellationToken cancellationToken = default)
        {
            return _context.DuAns
                .AsNoTracking()
                .AnyAsync(x => x.MaDuAn == projectId, cancellationToken);
        }

        public Task<bool> UserExistsAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return _context.NguoiDungs
                .AsNoTracking()
                .AnyAsync(x => x.MaNguoiDung == userId, cancellationToken);
        }

        public Task<bool> SprintBelongsToProjectAsync(
            int sprintId,
            int projectId,
            CancellationToken cancellationToken = default)
        {
            return _context.Sprints
                .AsNoTracking()
                .AnyAsync(x => x.MaSprint == sprintId && x.MaDuAn == projectId, cancellationToken);
        }

        private static bool IsDoneStatus(string? status)
        {
            return string.Equals(
                status?.Trim(),
                StatusDone,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
