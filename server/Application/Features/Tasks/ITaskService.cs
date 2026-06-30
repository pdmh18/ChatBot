using Application.Common.DTOs.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.DTOs;

namespace Application.Features.Tasks
{
    public interface ITaskService
    {
        Task<PagedResultDto<TaskListItemDto>> GetListAsync(
    TaskQueryParameters query,
    CancellationToken cancellationToken = default);

        Task<TaskDetailDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<int> CreateAsync(
            CreateTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            UpdateTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateStatusAsync(
            int id,
            UpdateTaskStatusRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> AssignAsync(
            int id,
            AssignTaskRequest request,
            CancellationToken cancellationToken = default);
    }
}
