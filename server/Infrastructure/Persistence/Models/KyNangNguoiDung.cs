using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class KyNangNguoiDung
{
    public int MaNguoiDung { get; set; }

    public int MaKyNang { get; set; }

    public int MucDoThanhThao { get; set; }

    public decimal? SoNamKinhNghiem { get; set; }

    public virtual KyNang MaKyNangNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
