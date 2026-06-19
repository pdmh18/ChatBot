using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class v_Dataset_PhatHienDiemNghen
{
    public int MaCongViec { get; set; }

    public string? TrangThaiHienTai { get; set; }

    public decimal? SoGioUocTinh { get; set; }

    public int? SoTaskBiAnhHuongPhiaSau { get; set; }

    public int Nhan_GhiNhanDiemNghen { get; set; }
}
