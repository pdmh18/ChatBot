using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class ChiSoLichSuDuAn
{
    public int MaChiSo { get; set; }

    public int MaDuAn { get; set; }

    public int? TongSoCongViec { get; set; }

    public int? SoCongViecHoanThanh { get; set; }

    public int? SoCongViecTreHan { get; set; }

    public int? SoCongViecBiChan { get; set; }

    public decimal? TongGioUocTinh { get; set; }

    public decimal? TongGioThucTe { get; set; }

    public decimal? NganSachDaDung { get; set; }

    public int? SoLoiPhatSinh { get; set; }

    public int? SoYeuCauThayDoi { get; set; }

    public int? QuyMoNhom { get; set; }

    public DateTime? NgayGhiNhan { get; set; }

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;
}
