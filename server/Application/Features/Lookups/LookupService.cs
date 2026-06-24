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
            new() { Id = 1, Name = "Can lam" },
new() { Id = 2, Name = "Dang lam" },
new() { Id = 3, Name = "Cho kiem tra" },
new() { Id = 4, Name = "Hoan thanh" },
new() { Id = 5, Name = "Bi chan" },
new() { Id = 6, Name = "Cho duyet" },
new() { Id = 7, Name = "Da duyet" },
new() { Id = 8, Name = "Bi tu choi" },
new() { Id = 9, Name = "Da huy" }
        };

            return Task.FromResult(statuses);
        }

        public Task<IReadOnlyList<LookupItemDto>> GetTaskPrioritiesAsync()
        {
            IReadOnlyList<LookupItemDto> priorities = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "Thap" },
new() { Id = 2, Name = "Trung binh" },
new() { Id = 3, Name = "Cao" },
new() { Id = 4, Name = "Khan cap" }
        };

            return Task.FromResult(priorities);
        }
    }
}
