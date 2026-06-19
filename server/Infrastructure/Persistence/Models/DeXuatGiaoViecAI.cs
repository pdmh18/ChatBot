using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DeXuatGiaoViecAI
{
    public int MaDeXuat { get; set; }

    public int MaCongViec { get; set; }

    public int MaNguoiDuocDeXuat { get; set; }

    public string TenMoHinh { get; set; } = null!;

    public decimal DiemPhuHop { get; set; }

    public decimal? DiemKyNang { get; set; }

    public decimal? DiemKhoiLuong { get; set; }

    public decimal? DiemKinhNghiem { get; set; }

    public string? LyDo { get; set; }

    public bool? DaChapNhan { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual CongViec MaCongViecNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDuocDeXuatNavigation { get; set; } = null!;
}
