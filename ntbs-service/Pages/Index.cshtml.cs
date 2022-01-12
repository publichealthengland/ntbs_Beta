﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;
using ntbs_service.Models.Projections;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Services;

namespace ntbs_service.Pages
{
    public class IndexModel : PageModel
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserService _userService;
        private readonly IHomepageKpiService _homepageKpiService;
        private readonly IUserHelper _userHelper;

        public IndexModel(INotificationRepository notificationRepository,
            IAlertRepository alertRepository,
            IAuthorizationService authorizationService,
            IUserService userService,
            IHomepageKpiService homepageKpiService,
            IUserHelper userHelper)
        {
            _notificationRepository = notificationRepository;
            _authorizationService = authorizationService;
            _alertRepository = alertRepository;
            _userService = userService;
            _homepageKpiService = homepageKpiService;
            _userHelper = userHelper;
        }

        public IList<AlertWithTbServiceForDisplay> Alerts { get; set; }
        public IList<NotificationForHomePage> DraftNotifications { get; set; }
        public IList<NotificationForHomePage> RecentNotifications { get; set; }
        public SelectList TbServices { get; set; }
        public SelectList KpiFilter { get; set; }
        public IEnumerable<HomepageKpi> HomepageKpiDetails { get; set; }

        public async Task OnGetAsync()
        {
            HttpContext.Session.SetTopLevelBreadcrumb("Index", "/Index");
            await SetUserNotificationsAsync();
            await SetUserAlertsAndTbServicesAsync();
            await SetHomepageKpiDetails();
        }

        private async Task SetHomepageKpiDetails()
        {
            var userPermissionsFilter = await _userService.GetUserPermissionsFilterAsync(User);

            if (userPermissionsFilter.Type == UserType.NationalTeam)
            {
                HomepageKpiDetails = await _homepageKpiService.GetKpiForAllPhec();
            }
            else if (userPermissionsFilter.IsInAtLeastOneRegion)
            {
                HomepageKpiDetails = await _homepageKpiService.GetKpiForPhec(userPermissionsFilter.IncludedPHECCodes);
            }
            else
            {
                HomepageKpiDetails = await _homepageKpiService.GetKpiForTbService(userPermissionsFilter.IncludedTBServiceCodes);
            }

            KpiFilter = new SelectList(HomepageKpiDetails.OrderBy(x => x.Name), nameof(HomepageKpi.Code), nameof(HomepageKpi.Name));
        }

        private async Task SetUserNotificationsAsync()
        {
            var draftNotificationsQueryable = _notificationRepository.GetDraftNotificationsIQueryable();
            var recentNotificationsQueryable = _notificationRepository.GetRecentNotificationsIQueryable();
            DraftNotifications =
                (await _authorizationService.FilterNotificationsByUserAsync(User, draftNotificationsQueryable))
                .Take(10)
                .SelectNotificationForHomePage()
                .ToList();
            RecentNotifications =
                (await _authorizationService.FilterNotificationsByUserAsync(User, recentNotificationsQueryable))
                .Take(10)
                .SelectNotificationForHomePage()
                .ToList();
        }

        private async Task SetUserAlertsAndTbServicesAsync()
        {
            if (_userService.GetUserType(User) == UserType.NationalTeam)
            {
                return;
            }

            var services = (await _userService.GetTbServicesAsync(User)).ToList();
            TbServices = new SelectList(services, nameof(TBService.Code), nameof(TBService.Name));
            var alertsForTbServices = await _alertRepository.GetOpenAlertsByTbServiceCodesAsync(services.Select(tb => tb.Code));
            Alerts = await _authorizationService.FilterAlertsForUserAsync(User, alertsForTbServices);
        }

        public bool UserIsReadOnly()
        {
            return _userHelper.UserIsReadOnly(User);
        }
    }
}
