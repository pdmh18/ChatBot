using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class v_Dataset_DuBaoTreHan
{
    public int MaCongViec { get; set; }

    public string? DoUuTien { get; set; }

    public decimal? SoGioUocTinh { get; set; }

    public int? SoNamKinhNghiemNhanSu { get; set; }

    public decimal? KhoiLuongHienTaiNhanSu { get; set; }

    public int SoCongViecPhuThuocTruoc { get; set; }

    public int Nhan_CoTreHan { get; set; }
}
