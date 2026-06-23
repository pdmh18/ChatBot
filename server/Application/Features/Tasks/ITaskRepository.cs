using Application.Common.DTOs.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Tasks
{
    public interface ITaskRepository
    {
        Task<IReadOnlyList<TaskListItemDto>> GetListAsync(
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

        Task<bool> ProjectExistsAsync(
            int projectId,
            CancellationToken cancellationToken = default);

        Task<bool> UserExistsAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<bool> SprintBelongsToProjectAsync(
            int sprintId,
            int projectId,
            CancellationToken cancellationToken = default);
    }
}
