using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class RuiRoNghiemTrong
{
    public string TenDuAn { get; set; } = null!;

    public string? TenCongViec { get; set; }

    public string TenLoaiRuiRo { get; set; } = null!;

    public decimal XacSuatRuiRo { get; set; }

    public string MucDoRuiRo { get; set; } = null!;

    public string? TacDongDuBao { get; set; }

    public string? KhuyenNghi { get; set; }

    public DateTime? NgayDuBao { get; set; }
}
