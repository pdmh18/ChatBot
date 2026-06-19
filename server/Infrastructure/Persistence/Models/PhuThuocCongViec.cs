using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class PhuThuocCongViec
{
    public int MaCongViecTruoc { get; set; }

    public int MaCongViecSau { get; set; }

    public string? LoaiPhuThuoc { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual CongViec MaCongViecSauNavigation { get; set; } = null!;

    public virtual CongViec MaCongViecTruocNavigation { get; set; } = null!;
}
