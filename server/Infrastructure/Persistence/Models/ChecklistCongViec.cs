using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class ChecklistCongViec
{
    public int MaChecklist { get; set; }

    public int MaCongViec { get; set; }

    public string NoiDung { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool? DaHoanThanh { get; set; }

    public int? MaNguoiHoanThanh { get; set; }

    public DateTime? NgayHoanThanh { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual CongViec MaCongViecNavigation { get; set; } = null!;

    public virtual NguoiDung? MaNguoiHoanThanhNavigation { get; set; }
}
