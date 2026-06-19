using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class KyNangCongViec
{
    public int MaCongViec { get; set; }

    public int MaKyNang { get; set; }

    public int MucDoYeuCau { get; set; }

    public virtual CongViec MaCongViecNavigation { get; set; } = null!;

    public virtual KyNang MaKyNangNavigation { get; set; } = null!;
}
