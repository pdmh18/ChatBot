using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class ThanhVienDuAn
{
    public int MaThanhVienDuAn { get; set; }

    public int MaDuAn { get; set; }

    public int MaNguoiDung { get; set; }

    public DateOnly? NgayThamGia { get; set; }

    public DateOnly? NgayRoiDuAn { get; set; }

    public int? TyLePhanBo { get; set; }

    public string? VaiTroTrongDuAn { get; set; }

    public virtual DuAn MaDuAnNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
