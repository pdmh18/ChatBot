using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class NhatKyLamViec
{
    public int MaNhatKy { get; set; }

    public int MaCongViec { get; set; }

    public int MaNguoiDung { get; set; }

    public DateOnly NgayLamViec { get; set; }

    public string NoiDungLamViec { get; set; } = null!;

    public decimal SoGioLam { get; set; }

    public int? TienDoSauKhiLam { get; set; }

    public string? VanDeGapPhai { get; set; }

    public string? KeHoachTiepTheo { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual CongViec MaCongViecNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
