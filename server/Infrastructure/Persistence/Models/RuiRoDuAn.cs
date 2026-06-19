using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class RuiRoDuAn
{
    public int MaRuiRo { get; set; }

    public int MaDuAn { get; set; }

    public int? MaCongViec { get; set; }

    public int MaLoaiRuiRo { get; set; }

    public string TieuDeRuiRo { get; set; } = null!;

    public string? MoTa { get; set; }

    public decimal? XacSuat { get; set; }

    public string? MucDoAnhHuong { get; set; }

    public string? TrangThai { get; set; }

    public string? PhuongAnGiamThieu { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual CongViec? MaCongViecNavigation { get; set; }

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;

    public virtual LoaiRuiRo MaLoaiRuiRoNavigation { get; set; } = null!;
}
