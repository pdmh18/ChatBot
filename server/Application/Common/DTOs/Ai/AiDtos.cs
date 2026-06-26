using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Ai
{
    public class TaskRiskAiRequest
    {
        [JsonPropertyName("SoGioUocTinh")]
        public double SoGioUocTinh { get; set; }

        [JsonPropertyName("SoNamKinhNghiemNhanSu")]
        public double SoNamKinhNghiemNhanSu { get; set; }

        [JsonPropertyName("KhoiLuongHienTaiNhanSu")]
        public double KhoiLuongHienTaiNhanSu { get; set; }

        [JsonPropertyName("SoCongViecPhuThuocTruoc")]
        public int SoCongViecPhuThuocTruoc { get; set; }

        [JsonPropertyName("DoUuTien_Encoded")]
        public int DoUuTienEncoded { get; set; }
    }

    public class TaskRiskAiResponse
    {
        [JsonPropertyName("xac_suat_tre_han")]
        public double XacSuatTreHan { get; set; }

        [JsonPropertyName("du_bao_tre_han")]
        public bool DuBaoTreHan { get; set; }

        [JsonPropertyName("muc_do_rui_ro")]
        public string MucDoRuiRo { get; set; } = string.Empty;

        [JsonPropertyName("nguyen_nhan")]
        public string? NguyenNhan { get; set; }
    }

    public class StaffMatchAiRequest
    {
        [JsonPropertyName("SoGioUocTinh")]
        public double SoGioUocTinh { get; set; }

        [JsonPropertyName("PhanTramTaiNhanSu")]
        public double PhanTramTaiNhanSu { get; set; }

        [JsonPropertyName("DiemChatLuongTrungBinhLichSu")]
        public double DiemChatLuongTrungBinhLichSu { get; set; }
    }

    public class StaffMatchAiResponse
    {
        [JsonPropertyName("xac_suat_hieu_qua")]
        public double XacSuatHieuQua { get; set; }

        [JsonPropertyName("de_xuat_giao_viec")]
        public bool DeXuatGiaoViec { get; set; }

        [JsonPropertyName("muc_do_phu_hop")]
        public string MucDoPhuHop { get; set; } = string.Empty;

        [JsonPropertyName("nguyen_nhan")]
        public string? NguyenNhan { get; set; }
    }

    public class BottleneckAiResponse
    {
        [JsonPropertyName("MaCongViec")]
        public int MaCongViec { get; set; }

        [JsonPropertyName("SoTaskBiAnhHuongPhiaSau")]
        public int SoTaskBiAnhHuongPhiaSau { get; set; }

        [JsonPropertyName("bottleneck_score")]
        public double BottleneckScore { get; set; }
    }

    // ============================================================
    // Internal data DTOs lấy từ DB để build request cho AI
    // ============================================================

    public class AiTaskDataDto
    {
        public int MaCongViec { get; set; }
        public int MaDuAn { get; set; }
        public string TenCongViec { get; set; } = string.Empty;
        public decimal SoGioUocTinh { get; set; }
        public string DoUuTien { get; set; } = "Trung binh";
        public int? MaNguoiPhuTrach { get; set; }
    }

    public class AiUserDataDto
    {
        public int MaNguoiDung { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public int SoNamKinhNghiem { get; set; }
        public decimal KhoiLuongHienTai { get; set; }
        public decimal KhoiLuongToiDa { get; set; }
        public decimal PhanTramTai { get; set; }
        public decimal DiemChatLuongTrungBinh { get; set; }
    }

    public class StaffMatchSaveItemDto
    {
        public AiUserDataDto User { get; set; } = null!;
        public StaffMatchAiResponse AiResult { get; set; } = new();
    }

    // ============================================================
    // DTOs trả về frontend sau khi đã lưu DB
    // ============================================================

    public class RiskPredictionResultDto
    {
        public int MaDuBao { get; set; }
        public int MaDuAn { get; set; }
        public int? MaCongViec { get; set; }
        public int MaLoaiRuiRo { get; set; }
        public string TenMoHinh { get; set; } = string.Empty;
        public decimal XacSuatRuiRo { get; set; }
        public bool DuBaoTreHan { get; set; }
        public string MucDoRuiRo { get; set; } = string.Empty;
        public string? TacDongDuBao { get; set; }
        public string? KhuyenNghi { get; set; }
        public DateTime? NgayDuBao { get; set; }
    }

    public class StaffMatchResultDto
    {
        public int MaDeXuat { get; set; }
        public int MaCongViec { get; set; }
        public int MaNguoiDuocDeXuat { get; set; }
        public string HoTenNguoiDuocDeXuat { get; set; } = string.Empty;
        public string TenMoHinh { get; set; } = string.Empty;
        public decimal DiemPhuHop { get; set; }
        public decimal? DiemKyNang { get; set; }
        public decimal? DiemKhoiLuong { get; set; }
        public decimal? DiemKinhNghiem { get; set; }
        public string? LyDo { get; set; }
        public bool? DaChapNhan { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    public class BottleneckResultDto
    {
        public int MaDiemNghen { get; set; }
        public int MaDuAn { get; set; }
        public int? MaCongViec { get; set; }
        public string KhuVucPhatHien { get; set; } = string.Empty;
        public string? NguyenNhan { get; set; }
        public string MucDoNghiemTrong { get; set; } = string.Empty;
        public int? SoNgayTreDuBao { get; set; }
        public string? KhuyenNghiAI { get; set; }
        public DateTime? NgayPhatHien { get; set; }
        public int SoTaskBiAnhHuongPhiaSau { get; set; }
        public double BottleneckScore { get; set; }
    }
}
