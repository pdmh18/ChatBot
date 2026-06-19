using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class ChiTietCongViec
{
    public int MaCongViec { get; set; }

    public string? MaCongViecCode { get; set; }

    public string TenCongViec { get; set; } = null!;

    public string TenDuAn { get; set; } = null!;

    public string NguoiTao { get; set; } = null!;

    public string? NguoiPhuTrach { get; set; }

    public string? VaiTroNguoiPhuTrach { get; set; }

    public string? DoUuTien { get; set; }

    public string? TrangThai { get; set; }

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? HanChot { get; set; }

    public DateOnly? NgayHoanThanh { get; set; }

    public decimal? SoGioUocTinh { get; set; }

    public decimal? SoGioThucTe { get; set; }

    public int? TienDo { get; set; }
}
