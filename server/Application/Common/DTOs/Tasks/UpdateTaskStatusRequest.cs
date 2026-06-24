using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Tasks
{
    public class UpdateTaskStatusRequest
    {
        [Required]
        public string TrangThai { get; set; } = string.Empty;

        [Range(0, 100)]
        public int? TienDo { get; set; }
    }
}
