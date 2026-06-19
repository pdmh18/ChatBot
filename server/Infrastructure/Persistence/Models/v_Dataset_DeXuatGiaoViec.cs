using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class v_Dataset_DeXuatGiaoViec
{
    public int MaCongViec { get; set; }

    public int? MaNguoiPhuTrach { get; set; }

    public decimal? SoGioUocTinh { get; set; }

    public decimal? PhanTramTaiNhanSu { get; set; }

    public decimal DiemChatLuongTrungBinhLichSu { get; set; }

    public int Nhan_GiaoViecHieuQua { get; set; }
}
