using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DanhGiaHieuSuatCongViec
{
    public int MaDanhGia { get; set; }

    public int MaCongViec { get; set; }

    public int MaNguoiDuocDanhGia { get; set; }

    public int MaNguoiDanhGia { get; set; }

    public int? DiemChatLuong { get; set; }

    public int? DiemTocDo { get; set; }

    public int? DiemGiaoTiep { get; set; }

    public int? DiemGiaiQuyetVanDe { get; set; }

    public string? NhanXet { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public virtual CongViec MaCongViecNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDanhGiaNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDuocDanhGiaNavigation { get; set; } = null!;
}
