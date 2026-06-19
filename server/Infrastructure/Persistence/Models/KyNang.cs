using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class KyNang
{
    public int MaKyNang { get; set; }

    public string TenKyNang { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<KyNangCongViec> KyNangCongViecs { get; set; } = new List<KyNangCongViec>();

    public virtual ICollection<KyNangNguoiDung> KyNangNguoiDungs { get; set; } = new List<KyNangNguoiDung>();
}
