using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Tasks
{
    public class CreateTaskRequest
    {
        [Required]
        public int MaDuAn { get; set; }

        public int? MaSprint { get; set; }

        public string? MaCongViecCode { get; set; }

        [Required]
        [MaxLength(255)]
        public string TenCongViec { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        [Required]
        public int MaNguoiTao { get; set; }

        public int? MaNguoiPhuTrach { get; set; }

        public string? DoUuTien { get; set; }

        public string? TrangThai { get; set; }

        public DateOnly? NgayBatDau { get; set; }

        public DateOnly? HanChot { get; set; }

        public decimal? SoGioUocTinh { get; set; }

        [Range(0, 100)]
        public int? TienDo { get; set; }
    }
}
