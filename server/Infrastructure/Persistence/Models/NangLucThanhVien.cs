using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class NangLucThanhVien
{
    public int MaNguoiDung { get; set; }

    public string HoTen { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public int? TongCongViecDuocGiao { get; set; }

    public int? SoCongViecHoanThanh { get; set; }

    public int? SoCongViecTreHan { get; set; }

    public decimal? TongGioUocTinh { get; set; }

    public decimal? TongGioThucTe { get; set; }

    public decimal? DiemChatLuongTrungBinh { get; set; }

    public decimal? DiemTocDoTrungBinh { get; set; }
}
