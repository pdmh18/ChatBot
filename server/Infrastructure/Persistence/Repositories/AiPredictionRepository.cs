using Application.Common.DTOs.Ai;
using Application.Features.Ai;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class AiPredictionRepository : IAiPredictionRepository
    {
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
                    TenCongViec = x.TenCongViec,
                    SoGioUocTinh = x.SoGioUocTinh ?? 0m,
                    DoUuTien = x.DoUuTien ?? "Trung binh",
                    MaNguoiPhuTrach = x.MaNguoiPhuTrach
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AiUserDataDto?> GetUserDataAsync(
            int userId,
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
                    KhoiLuongHienTai = x.KhoiLuongHienTai ?? 0m,
                    KhoiLuongToiDa = x.KhoiLuongToiDa ?? 40m,
                    DiemChatLuongTrungBinh = 5m
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return null;
            }

            var qualityScore = await _context.NangLucThanhViens
                .AsNoTracking()
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.DiemChatLuongTrungBinh)
                .FirstOrDefaultAsync(cancellationToken);

            user.DiemChatLuongTrungBinh = qualityScore ?? 5m;
            user.PhanTramTai = CalculateWorkloadRatio(user.KhoiLuongHienTai, user.KhoiLuongToiDa);

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
            await _context.SaveChangesAsync(cancellationToken);

            return entity.MaLoaiRuiRo;
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
                TacDongDuBao = aiResult.DuBaoTreHan
                    ? "Task co nguy co tre han"
                    : "Nguy co tre han thap",
                KhuyenNghi = aiResult.DuBaoTreHan
                    ? "Can xem lai han chot, khoi luong va nguoi phu trach."
                    : "Tiep tuc theo doi tien do task.",
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
            var entity = new DeXuatGiaoViecAI
            {
                MaCongViec = task.MaCongViec,
                MaNguoiDuocDeXuat = user.MaNguoiDung,
                TenMoHinh = "Random Forest Staff Matching",
                DiemPhuHop = Round2(aiResult.XacSuatHieuQua),
                DiemKyNang = Round2(user.DiemChatLuongTrungBinh),
                DiemKhoiLuong = Round2(1m - user.PhanTramTai),
                DiemKinhNghiem = Round2((decimal)user.SoNamKinhNghiem),
                LyDo = BuildStaffReason(aiResult, user),
                DaChapNhan = false,
                NgayTao = DateTime.UtcNow
            };

            _context.DeXuatGiaoViecAIs.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return new StaffMatchResultDto
            {
                MaDeXuat = entity.MaDeXuat,
                MaCongViec = entity.MaCongViec,
                MaNguoiDuocDeXuat = entity.MaNguoiDuocDeXuat,
                HoTenNguoiDuocDeXuat = user.HoTen,
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

        public async Task<IReadOnlyList<BottleneckResultDto>> SaveBottleneckResultsAsync(
            IReadOnlyList<BottleneckAiResponse> aiResults,
            CancellationToken cancellationToken = default)
        {
            var savedResults = new List<BottleneckResultDto>();

            foreach (var result in aiResults)
            {
                var task = await _context.CongViecs
                    .AsNoTracking()
                    .Where(x => x.MaCongViec == result.MaCongViec)
                    .Select(x => new
                    {
                        x.MaCongViec,
                        x.MaDuAn
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (task == null)
                {
                    continue;
                }

                var entity = new DiemNghenAI
                {
                    MaDuAn = task.MaDuAn,
                    MaCongViec = task.MaCongViec,
                    KhuVucPhatHien = "Cong viec phu thuoc",
                    NguyenNhan = $"Task anh huong {result.SoTaskBiAnhHuongPhiaSau} task phia sau. Bottleneck score = {result.BottleneckScore:0.0000}",
                    MucDoNghiemTrong = GetBottleneckSeverity(result.BottleneckScore),
                    SoNgayTreDuBao = 0,
                    KhuyenNghiAI = BuildBottleneckRecommendation(result),
                    NgayPhatHien = DateTime.UtcNow
                };

                _context.DiemNghenAIs.Add(entity);
                await _context.SaveChangesAsync(cancellationToken);

                savedResults.Add(new BottleneckResultDto
                {
                    MaDiemNghen = entity.MaDiemNghen,
                    MaDuAn = entity.MaDuAn,
                    MaCongViec = entity.MaCongViec,
                    KhuVucPhatHien = entity.KhuVucPhatHien,
                    NguyenNhan = entity.NguyenNhan,
                    MucDoNghiemTrong = entity.MucDoNghiemTrong,
                    SoNgayTreDuBao = entity.SoNgayTreDuBao,
                    KhuyenNghiAI = entity.KhuyenNghiAI,
                    NgayPhatHien = entity.NgayPhatHien
                });
            }

            return savedResults;
        }

        private static decimal CalculateWorkloadRatio(decimal current, decimal max)
        {
            if (max <= 0)
            {
                return 0m;
            }

            var ratio = current / max;

            if (ratio < 0m) return 0m;
            if (ratio > 1m) return 1m;

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
            return value switch
            {
                "Thấp" => "Thap",
                "Trung bình" => "Trung binh",
                "Cao" => "Cao",
                _ => value ?? "Khong xac dinh"
            };
        }

        private static string BuildStaffReason(StaffMatchAiResponse aiResult, AiUserDataDto user)
        {
            var decision = aiResult.DeXuatGiaoViec
                ? "AI de xuat giao viec"
                : "AI khong de xuat giao viec";

            return $"{decision}. Muc do phu hop: {aiResult.MucDoPhuHop}. Tai hien tai: {user.PhanTramTai:P0}.";
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
    }
}
