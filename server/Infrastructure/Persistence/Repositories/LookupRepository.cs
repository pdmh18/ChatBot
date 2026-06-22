using Application.Common.DTOs.Lookups;
using Application.Features.Lookups;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly QuanLyDuAnAiContext _context;

    public LookupRepository(QuanLyDuAnAiContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DuAns
            .AsNoTracking()
            .OrderBy(x => x.TenDuAn)
            .Select(x => new LookupItemDto
            {
                Id = x.MaDuAn,
                Name = x.TenDuAn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserLookupDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NguoiDungs
            .AsNoTracking()
            .Where(x => x.DangHoatDong != false)
            .OrderBy(x => x.HoTen)
            .Select(x => new UserLookupDto
            {
                Id = x.MaNguoiDung,
                HoTen = x.HoTen,
                Email = x.Email,
                VaiTro = x.MaVaiTroNavigation.TenVaiTro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SprintLookupDto>> GetSprintsAsync(
        int? projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Sprints
            .AsNoTracking()
            .Where(x => projectId == null || x.MaDuAn == projectId.Value)
            .OrderBy(x => x.NgayBatDau)
            .Select(x => new SprintLookupDto
            {
                Id = x.MaSprint,
                TenSprint = x.TenSprint,
                MaDuAn = x.MaDuAn,
                TenDuAn = x.MaDuAnNavigation.TenDuAn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VaiTros
            .AsNoTracking()
            .OrderBy(x => x.TenVaiTro)
            .Select(x => new LookupItemDto
            {
                Id = x.MaVaiTro,
                Name = x.TenVaiTro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.KyNangs
            .AsNoTracking()
            .OrderBy(x => x.TenKyNang)
            .Select(x => new LookupItemDto
            {
                Id = x.MaKyNang,
                Name = x.TenKyNang
            })
            .ToListAsync(cancellationToken);
    }
}