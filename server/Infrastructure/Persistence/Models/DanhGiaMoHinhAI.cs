using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class DanhGiaMoHinhAI
{
    public int MaDanhGiaMoHinh { get; set; }

    public string TenMoHinh { get; set; } = null!;

    public string? PhienBanMoHinh { get; set; }

    public string? LoaiDuBao { get; set; }

    public decimal? DoChinhXac { get; set; }

    public decimal? PrecisionScore { get; set; }

    public decimal? RecallScore { get; set; }

    public decimal? F1Score { get; set; }

    public DateTime? NgayDanhGia { get; set; }
}
