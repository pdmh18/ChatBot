using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTOs.Tasks
{
    public class TaskQueryParameters
    {
        private const int MaxPageSize = 100;

        private int _pageNumber = 1;
        private int _pageSize = 20;

        public string? Search { get; set; }

        public int? ProjectId { get; set; }

        public int? SprintId { get; set; }

        public int? AssigneeId { get; set; }

        public string? Status { get; set; }

        public string? Priority { get; set; }

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1)
                {
                    _pageSize = 20;
                    return;
                }

                _pageSize = value > MaxPageSize ? MaxPageSize : value;
            }
        }
    }
}
