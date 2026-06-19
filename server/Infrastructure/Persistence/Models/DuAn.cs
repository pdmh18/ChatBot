using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DuAn
{
    public int MaDuAn { get; set; }

    public string TenDuAn { get; set; } = null!;

    public string? MoTa { get; set; }

    public int MaQuanLyDuAn { get; set; }

    public DateOnly NgayBatDau { get; set; }

    public DateOnly? NgayKetThucDuKien { get; set; }

    public DateOnly? NgayKetThucThucTe { get; set; }

    public decimal? NganSach { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<ChiSoLichSuDuAn> ChiSoLichSuDuAns { get; set; } = new List<ChiSoLichSuDuAn>();

    public virtual ICollection<CongViec> CongViecs { get; set; } = new List<CongViec>();

    public virtual ICollection<DiemNghenAI> DiemNghenAIs { get; set; } = new List<DiemNghenAI>();

    public virtual ICollection<DuBaoRuiRoAI> DuBaoRuiRoAIs { get; set; } = new List<DuBaoRuiRoAI>();

    public virtual ICollection<LichSuHoatDong> LichSuHoatDongs { get; set; } = new List<LichSuHoatDong>();

    public virtual NguoiDung MaQuanLyDuAnNavigation { get; set; } = null!;

    public virtual ICollection<RuiRoDuAn> RuiRoDuAns { get; set; } = new List<RuiRoDuAn>();

    public virtual ICollection<SnapshotTienDoDuAn> SnapshotTienDoDuAns { get; set; } = new List<SnapshotTienDoDuAn>();

    public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

    public virtual ICollection<TaiLieu> TaiLieus { get; set; } = new List<TaiLieu>();

    public virtual ICollection<ThanhVienDuAn> ThanhVienDuAns { get; set; } = new List<ThanhVienDuAn>();
}
