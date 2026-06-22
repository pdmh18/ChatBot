using Application.Common.DTOs.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Lookups
{
    public class LookupService : ILookupService
    {
        private readonly ILookupRepository _lookupRepository;

        public LookupService(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository;
        }

        public Task<IReadOnlyList<LookupItemDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
        {
            return _lookupRepository.GetProjectsAsync(cancellationToken);
        }

        public Task<IReadOnlyList<UserLookupDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return _lookupRepository.GetUsersAsync(cancellationToken);
        }

        public Task<IReadOnlyList<SprintLookupDto>> GetSprintsAsync(
            int? projectId,
            CancellationToken cancellationToken = default)
        {
            return _lookupRepository.GetSprintsAsync(projectId, cancellationToken);
        }

        public Task<IReadOnlyList<LookupItemDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return _lookupRepository.GetRolesAsync(cancellationToken);
        }

        public Task<IReadOnlyList<LookupItemDto>> GetSkillsAsync(CancellationToken cancellationToken = default)
        {
            return _lookupRepository.GetSkillsAsync(cancellationToken);
        }

        public Task<IReadOnlyList<LookupItemDto>> GetTaskStatusesAsync()
        {
            IReadOnlyList<LookupItemDto> statuses = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "Todo" },
            new() { Id = 2, Name = "In Progress" },
            new() { Id = 3, Name = "Review" },
            new() { Id = 4, Name = "Done" }
        };

            return Task.FromResult(statuses);
        }

        public Task<IReadOnlyList<LookupItemDto>> GetTaskPrioritiesAsync()
        {
            IReadOnlyList<LookupItemDto> priorities = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "Low" },
            new() { Id = 2, Name = "Medium" },
            new() { Id = 3, Name = "High" }
        };

            return Task.FromResult(priorities);
        }
    }
}
