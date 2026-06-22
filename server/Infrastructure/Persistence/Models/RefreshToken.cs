using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class RefreshToken
{
    public long Id { get; set; }

    public string RefreshToken1 { get; set; } = null!;

    public DateTime ThoiGianHetHan { get; set; }

    public int MaNguoiDung { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
