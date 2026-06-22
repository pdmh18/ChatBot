using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class NguoiDung
{
    public int MaNguoiDung { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public int MaVaiTro { get; set; }

    public int? SoNamKinhNghiem { get; set; }

    public decimal? KhoiLuongHienTai { get; set; }

    public decimal? KhoiLuongToiDa { get; set; }

    public bool? DangHoatDong { get; set; }

    public DateTime? NgayTao { get; set; }

    public decimal? MucLuongTheoGio { get; set; }

    public string? Password { get; set; }

    public virtual ICollection<ChecklistCongViec> ChecklistCongViecs { get; set; } = new List<ChecklistCongViec>();

    public virtual ICollection<CongViec> CongViecMaNguoiPhuTrachNavigations { get; set; } = new List<CongViec>();

    public virtual ICollection<CongViec> CongViecMaNguoiTaoNavigations { get; set; } = new List<CongViec>();

    public virtual ICollection<DanhGiaHieuSuatCongViec> DanhGiaHieuSuatCongViecMaNguoiDanhGiaNavigations { get; set; } = new List<DanhGiaHieuSuatCongViec>();

    public virtual ICollection<DanhGiaHieuSuatCongViec> DanhGiaHieuSuatCongViecMaNguoiDuocDanhGiaNavigations { get; set; } = new List<DanhGiaHieuSuatCongViec>();

    public virtual ICollection<DeXuatGiaoViecAI> DeXuatGiaoViecAIs { get; set; } = new List<DeXuatGiaoViecAI>();

    public virtual ICollection<DuAn> DuAns { get; set; } = new List<DuAn>();

    public virtual ICollection<DuyetCongViec> DuyetCongViecMaNguoiDuyetNavigations { get; set; } = new List<DuyetCongViec>();

    public virtual ICollection<DuyetCongViec> DuyetCongViecMaNguoiYeuCauNavigations { get; set; } = new List<DuyetCongViec>();

    public virtual ICollection<KyNangNguoiDung> KyNangNguoiDungs { get; set; } = new List<KyNangNguoiDung>();

    public virtual ICollection<LichSuHoatDong> LichSuHoatDongs { get; set; } = new List<LichSuHoatDong>();

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;

    public virtual ICollection<NhatKyLamViec> NhatKyLamViecs { get; set; } = new List<NhatKyLamViec>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<TaiLieu> TaiLieus { get; set; } = new List<TaiLieu>();

    public virtual ICollection<ThanhVienDuAn> ThanhVienDuAns { get; set; } = new List<ThanhVienDuAn>();
}
