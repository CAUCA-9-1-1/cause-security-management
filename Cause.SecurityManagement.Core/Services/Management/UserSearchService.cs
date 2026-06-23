using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class UserSearchService<TUser>(
        ISecurityContext<TUser> context,
        IUserAdditionalInformationProvider<TUser> additionalInformationProvider)
        : IUserSearchService
        where TUser : User, new()
    {
        public async Task<UserSearchResultDto> SearchUsersAsync(UserSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = context.Users.AsNoTracking().Where(user => user.IsActive);

            var excludedUserIds = request.ExcludedUserIds ?? new List<Guid>();
            if (excludedUserIds.Count > 0)
                query = query.Where(user => !excludedUserIds.Contains(user.Id));

            var term = (request.Query ?? string.Empty).Trim().ToLower();
            if (term.Length > 0)
                query = query.Where(user =>
                    (user.FirstName != null && user.FirstName.ToLower().Contains(term))
                    || (user.LastName != null && user.LastName.ToLower().Contains(term)));

            var totalCount = await query.CountAsync(cancellationToken);

            var pagedQuery = query
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .Skip(request.Skip);
            if (request.Top > 0)
                pagedQuery = pagedQuery.Take(request.Top);

            Expression<Func<TUser, UserForGroupDto>> baseProjection = user => new UserForGroupDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            var items = await pagedQuery
                .Select(baseProjection.WithAdditionalInformation(additionalInformationProvider.GetAdditionalInformation()))
                .ToListAsync(cancellationToken);

            return new UserSearchResultDto { Items = items, TotalCount = totalCount };
        }
    }
}
