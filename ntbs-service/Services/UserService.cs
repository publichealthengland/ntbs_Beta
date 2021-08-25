﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Properties;

namespace ntbs_service.Services
{
    public interface IUserService
    {
        Task<UserPermissionsFilter> GetUserPermissionsFilterAsync(ClaimsPrincipal user);
        Task<TBService> GetDefaultTbService(ClaimsPrincipal user);
        Task<IEnumerable<TBService>> GetTbServicesAsync(ClaimsPrincipal user);
        UserType GetUserType(ClaimsPrincipal user);
        Task<IEnumerable<string>> GetPhecCodesAsync(ClaimsPrincipal user);
        Task RecordUserLoginAsync(string username);
        Task<User> GetUser(ClaimsPrincipal user);
        string GetUserDisplayName(ClaimsPrincipal user);
    }

    public class UserService : IUserService
    {
        private readonly AdOptions _config;
        private readonly IReferenceDataRepository _referenceDataRepository;
        private readonly IUserRepository _userRepository;

        public UserService(
            IReferenceDataRepository referenceDataRepository,
            IUserRepository userRepository,
            IOptionsMonitor<AdOptions> options)
        {
            _referenceDataRepository = referenceDataRepository;
            _userRepository = userRepository;
            _config = options.CurrentValue;
        }

        public async Task<UserPermissionsFilter> GetUserPermissionsFilterAsync(ClaimsPrincipal user)
        {
            var userFilter = new UserPermissionsFilter { Type = GetUserType(user) };

            if (userFilter.Type == UserType.NationalTeam)
            {
                // National Team users have access to all services' records
                return userFilter;
            }

            var roles = GetRoles(user);
            if (userFilter.Type == UserType.NhsUser)
            {
                userFilter.IncludedTBServiceCodes = await _referenceDataRepository.GetTbServiceCodesMatchingRolesAsync(roles);
            }
            else
            {
                userFilter.IncludedPHECCodes = await _referenceDataRepository.GetPhecCodesMatchingRolesAsync(roles);
            }

            return userFilter;
        }

        public async Task<TBService> GetDefaultTbService(ClaimsPrincipal user)
        {
            return await GetTbServicesQuery(user).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TBService>> GetTbServicesAsync(ClaimsPrincipal user)
        {
            return await GetTbServicesQuery(user).ToListAsync();
        }

        public async Task<User> GetUser(ClaimsPrincipal user)
        {
            return await _userRepository.GetUserByUsername(user.Username());
        }

        public UserType GetUserType(ClaimsPrincipal user)
        {
            if (user.IsInRole(_config.NationalTeamAdGroup))
            {
                return UserType.NationalTeam;
            }

            if (GetRoles(user).Any(role => role.Contains(_config.ServiceGroupAdPrefix)))
            {
                return UserType.NhsUser;
            }

            return UserType.PheUser;
        }

        public async Task<IEnumerable<string>> GetPhecCodesAsync(ClaimsPrincipal user)
        {
            if (GetUserType(user) == UserType.NationalTeam)
            {
                return (await _referenceDataRepository.GetAllPhecs())?.Select(x => x.Code);
            }
            return await _referenceDataRepository.GetPhecCodesMatchingRolesAsync(GetRoles(user));
        }

        public async Task RecordUserLoginAsync(string username)
        {
            await _userRepository.AddUserLoginEvent(new UserLoginEvent()
            {
                SystemName = "NTBS",
                Username = username,
                LoginDate = DateTime.Now
            });
        }

        public string GetUserDisplayName(ClaimsPrincipal user)
        {
            var displayName = NameFormattingHelper.FormatDisplayName(user.Identity?.Name);

            if (displayName.IsNullOrEmpty() || displayName.Contains("@"))
            {
                var nameClaim = user.Claims.FirstOrDefault(c => c.Type == "name");
                if (nameClaim != null)
                {
                    displayName = NameFormattingHelper.FormatDisplayName(nameClaim.Value);
                }
            }

            return displayName;
        }

        private IQueryable<TBService> GetTbServicesQuery(ClaimsPrincipal user)
        {
            var type = GetUserType(user);
            var roles = GetRoles(user);

            switch (type)
            {
                case UserType.NationalTeam:
                    return _referenceDataRepository.GetTbServicesQueryable();
                case UserType.NhsUser:
                    return _referenceDataRepository.GetDefaultTbServicesForNhsUserQueryable(roles);
                default:
                    return _referenceDataRepository.GetDefaultTbServicesForPheUserQueryable(roles);
            }
        }

        private static IEnumerable<string> GetRoles(ClaimsPrincipal user)
        {
            return user.FindAll(claim => claim.Type == ClaimsIdentity.DefaultRoleClaimType)
                .Select(claim => claim.Value);
        }
    }
}
