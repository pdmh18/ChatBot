using Application.Common.DTOs.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Lookups
{
    public interface ILookupRepository
    {
        Task<IReadOnlyList<LookupItemDto>> GetProjectsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserLookupDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SprintLookupDto>> GetSprintsAsync(int? projectId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LookupItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LookupItemDto>> GetSkillsAsync(CancellationToken cancellationToken = default);
    }
}
