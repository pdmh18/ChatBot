using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class SnapshotTienDoDuAn
{
    public int MaSnapshot { get; set; }

    public int MaDuAn { get; set; }

    public DateOnly? NgayGhiNhan { get; set; }

    public int? TongTaskDangMo { get; set; }

    public int? TongTaskHoanThanh { get; set; }

    public int? TongTaskBiChan { get; set; }

    public int? TongTaskTreHan { get; set; }

    public decimal? TongGioUocTinhConLai { get; set; }

    public decimal? TongChiPhiThucTe { get; set; }

    public int? SoNhanSuDangHoatDong { get; set; }

    public string? GhiChu { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;
}
