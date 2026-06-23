using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Tasks
{
    public class TaskDetailDto
    {
        public int MaCongViec { get; set; }

        public string? MaCongViecCode { get; set; }

        public string TenCongViec { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        public int MaDuAn { get; set; }

        public string TenDuAn { get; set; } = string.Empty;

        public int MaNguoiTao { get; set; }

        public string NguoiTao { get; set; } = string.Empty;

        public int? MaNguoiPhuTrach { get; set; }

        public string? NguoiPhuTrach { get; set; }

        public int? MaSprint { get; set; }

        public string? TenSprint { get; set; }

        public string? DoUuTien { get; set; }

        public string? TrangThai { get; set; }

        public DateOnly? NgayBatDau { get; set; }

        public DateOnly? HanChot { get; set; }

        public DateOnly? NgayHoanThanh { get; set; }

        public decimal? SoGioUocTinh { get; set; }

        public decimal? SoGioThucTe { get; set; }

        public int? TienDo { get; set; }

        public DateTime? NgayTao { get; set; }

        public DateTime? NgayCapNhat { get; set; }

        public int RiskPercent { get; set; }

        public string RiskLevel { get; set; } = "Low";
    }
}
