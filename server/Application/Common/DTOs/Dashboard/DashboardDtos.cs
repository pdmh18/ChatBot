using System;

namespace Application.Common.DTOs.Dashboard
{
    public class DashboardDtos
    {
        public class DashboardSummaryDto
        {
            public int TongCongViec { get; set; }
            public int TaskNguyCoTreHan { get; set; }
            public int DiemNghen { get; set; }
            public int TaskChuaPhanCong { get; set; }
            public int TaskHoanThanh { get; set; }
            public int TaskDangLam { get; set; }
        }

        public class DashboardWorkloadDto
        {
            public int MaNguoiDung { get; set; }
            public string HoTen { get; set; } = string.Empty;
            public int SoTask { get; set; }
            public decimal TongGioUocTinh { get; set; }

            // Đơn vị: phần trăm, ví dụ 75 nghĩa là 75%.
            public decimal PhanTramTai { get; set; }

            public string MucDoTai { get; set; } = string.Empty;
        }

        public class DashboardTaskAlertDto
        {
            public string LoaiCanhBao { get; set; } = string.Empty;

            public int MaCongViec { get; set; }
            public string? MaCongViecCode { get; set; }
            public string TenCongViec { get; set; } = string.Empty;

            public int MaDuAn { get; set; }
            public int? MaSprint { get; set; }

            public int? MaNguoiPhuTrach { get; set; }
            public string? NguoiPhuTrach { get; set; }

            public string? TrangThai { get; set; }
            public string? DoUuTien { get; set; }

            public DateOnly? NgayBatDau { get; set; }
            public DateOnly? HanChot { get; set; }
            public int? TienDo { get; set; }
            public decimal? SoGioUocTinh { get; set; }

            public int RiskPercent { get; set; }
            public string RiskLevel { get; set; } = string.Empty;

            public string NguyenNhan { get; set; } = string.Empty;
            public string KhuyenNghi { get; set; } = string.Empty;
        }
    }
}
