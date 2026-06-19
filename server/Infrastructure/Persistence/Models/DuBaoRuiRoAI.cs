using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DuBaoRuiRoAI
{
    public int MaDuBao { get; set; }

    public int MaDuAn { get; set; }

    public int? MaCongViec { get; set; }

    public int MaLoaiRuiRo { get; set; }

    public string TenMoHinh { get; set; } = null!;

    public string? PhienBanMoHinh { get; set; }

    public decimal XacSuatRuiRo { get; set; }

    public string MucDoRuiRo { get; set; } = null!;

    public string? TacDongDuBao { get; set; }

    public string? KhuyenNghi { get; set; }

    public DateTime? NgayDuBao { get; set; }

    public virtual CongViec? MaCongViecNavigation { get; set; }

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;

    public virtual LoaiRuiRo MaLoaiRuiRoNavigation { get; set; } = null!;
}
