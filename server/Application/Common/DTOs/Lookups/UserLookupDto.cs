using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Lookups
{
    public class UserLookupDto
    {
        public int Id { get; set; }

        public string HoTen { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string VaiTro { get; set; } = string.Empty;
    }
}
