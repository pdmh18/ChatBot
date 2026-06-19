using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class CongViec
{
    public int MaCongViec { get; set; }

    public int MaDuAn { get; set; }

    public string? MaCongViecCode { get; set; }

    public string TenCongViec { get; set; } = null!;

    public string? MoTa { get; set; }

    public int MaNguoiTao { get; set; }

    public int? MaNguoiPhuTrach { get; set; }

    public string? DoUuTien { get; set; }

    public string? TrangThai { get; set; }

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? HanChot { get; set; }

    public DateOnly? NgayHoanThanh { get; set; }

    public decimal? SoGioUocTinh { get; set; }

    public decimal? SoGioThucTe { get; set; }

    public int? TienDo { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public int? MaSprint { get; set; }

    public virtual ICollection<ChecklistCongViec> ChecklistCongViecs { get; set; } = new List<ChecklistCongViec>();

    public virtual DanhGiaHieuSuatCongViec? DanhGiaHieuSuatCongViec { get; set; }

    public virtual ICollection<DeXuatGiaoViecAI> DeXuatGiaoViecAIs { get; set; } = new List<DeXuatGiaoViecAI>();

    public virtual ICollection<DiemNghenAI> DiemNghenAIs { get; set; } = new List<DiemNghenAI>();

    public virtual ICollection<DuBaoRuiRoAI> DuBaoRuiRoAIs { get; set; } = new List<DuBaoRuiRoAI>();

    public virtual ICollection<DuyetCongViec> DuyetCongViecs { get; set; } = new List<DuyetCongViec>();

    public virtual ICollection<KyNangCongViec> KyNangCongViecs { get; set; } = new List<KyNangCongViec>();

    public virtual ICollection<LichSuHoatDong> LichSuHoatDongs { get; set; } = new List<LichSuHoatDong>();

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;

    public virtual NguoiDung? MaNguoiPhuTrachNavigation { get; set; }

    public virtual NguoiDung MaNguoiTaoNavigation { get; set; } = null!;

    public virtual Sprint? MaSprintNavigation { get; set; }

    public virtual ICollection<NhatKyLamViec> NhatKyLamViecs { get; set; } = new List<NhatKyLamViec>();

    public virtual ICollection<PhuThuocCongViec> PhuThuocCongViecMaCongViecSauNavigations { get; set; } = new List<PhuThuocCongViec>();

    public virtual ICollection<PhuThuocCongViec> PhuThuocCongViecMaCongViecTruocNavigations { get; set; } = new List<PhuThuocCongViec>();

    public virtual ICollection<RuiRoDuAn> RuiRoDuAns { get; set; } = new List<RuiRoDuAn>();

    public virtual ICollection<TaiLieu> TaiLieus { get; set; } = new List<TaiLieu>();
}
