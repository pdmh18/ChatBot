using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class ThanhVienQuaTai
{
    public int MaNguoiDung { get; set; }

    public string HoTen { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public decimal? KhoiLuongHienTai { get; set; }

    public decimal? KhoiLuongToiDa { get; set; }

    public decimal? PhanTramTai { get; set; }
}
