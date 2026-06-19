using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class TaiLieu
{
    public int MaTaiLieu { get; set; }

    public int? MaDuAn { get; set; }

    public int? MaCongViec { get; set; }

    public int MaNguoiTaiLen { get; set; }

    public string TenTaiLieu { get; set; } = null!;

    public string? LoaiTaiLieu { get; set; }

    public string DuongDan { get; set; } = null!;

    public string? MoTa { get; set; }

    public DateTime? NgayTaiLen { get; set; }

    public virtual CongViec? MaCongViecNavigation { get; set; }

    public virtual DuAn? MaDuAnNavigation { get; set; }

    public virtual NguoiDung MaNguoiTaiLenNavigation { get; set; } = null!;
}
