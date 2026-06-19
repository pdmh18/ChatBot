using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class LichSuHoatDong
{
    public int MaHoatDong { get; set; }

    public int? MaDuAn { get; set; }

    public int? MaCongViec { get; set; }

    public int MaNguoiDung { get; set; }

    public string LoaiHanhDong { get; set; } = null!;

    public string? GiaTriCu { get; set; }

    public string? GiaTriMoi { get; set; }

    public string? MoTa { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual CongViec? MaCongViecNavigation { get; set; }

    public virtual DuAn? MaDuAnNavigation { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
