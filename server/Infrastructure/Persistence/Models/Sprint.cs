using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class Sprint
{
    public int MaSprint { get; set; }

    public int MaDuAn { get; set; }

    public string TenSprint { get; set; } = null!;

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public string? MucTieu { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<CongViec> CongViecs { get; set; } = new List<CongViec>();

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;
}
