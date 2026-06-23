using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Tasks
{
    public class TaskQueryParameters
    {
        public string? Search { get; set; }

        public int? ProjectId { get; set; }

        public int? SprintId { get; set; }

        public int? AssigneeId { get; set; }

        public string? Status { get; set; }

        public string? Priority { get; set; }
    }
}
