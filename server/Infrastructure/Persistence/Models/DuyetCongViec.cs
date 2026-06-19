using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DuyetCongViec
{
    public int MaDuyet { get; set; }

    public int MaCongViec { get; set; }

    public int MaNguoiYeuCau { get; set; }

    public int? MaNguoiDuyet { get; set; }

    public string? TrangThaiDuyet { get; set; }

    public string? GhiChuYeuCau { get; set; }

    public string? GhiChuDuyet { get; set; }

    public DateTime? NgayYeuCau { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public virtual CongViec MaCongViecNavigation { get; set; } = null!;

    public virtual NguoiDung? MaNguoiDuyetNavigation { get; set; }

    public virtual NguoiDung MaNguoiYeuCauNavigation { get; set; } = null!;
}
