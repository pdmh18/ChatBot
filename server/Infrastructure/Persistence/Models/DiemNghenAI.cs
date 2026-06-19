using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DiemNghenAI
{
    public int MaDiemNghen { get; set; }

    public int MaDuAn { get; set; }

    public int? MaCongViec { get; set; }

    public string KhuVucPhatHien { get; set; } = null!;

    public string? NguyenNhan { get; set; }

    public string MucDoNghiemTrong { get; set; } = null!;

    public int? SoNgayTreDuBao { get; set; }

    public string? KhuyenNghiAI { get; set; }

    public DateTime? NgayPhatHien { get; set; }

    public virtual CongViec? MaCongViecNavigation { get; set; }

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;
}
