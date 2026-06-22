using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Lookups
{
    public class SprintLookupDto
    {
        public int Id { get; set; }

        public string TenSprint { get; set; } = string.Empty;

        public int MaDuAn { get; set; }

        public string TenDuAn { get; set; } = string.Empty;
    }
}
