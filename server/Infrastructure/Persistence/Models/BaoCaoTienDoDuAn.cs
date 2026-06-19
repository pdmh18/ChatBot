using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class BaoCaoTienDoDuAn
{
    public int MaDuAn { get; set; }

    public string TenDuAn { get; set; } = null!;

    public int? TongSoCongViec { get; set; }

    public int? SoCongViecHoanThanh { get; set; }

    public int? SoCongViecBiChan { get; set; }

    public decimal? PhanTramHoanThanh { get; set; }
}
