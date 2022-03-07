﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;
using ntbs_service.Services;

namespace ntbs_service.Pages.Notifications
{
    public abstract class NotificationModelBase : PageModel, IWithNotificationBanner
    {
        protected INotificationService Service;
        protected IAuthorizationService _authorizationService;
        protected INotificationRepository NotificationRepository;
        protected IUserHelper _userHelper;

        protected NotificationModelBase(
            INotificationService service,
            IAuthorizationService authorizationService,
            IUserHelper userHelper,
            INotificationRepository notificationRepository = null)
        {
            Service = service;
            _authorizationService = authorizationService;
            _userHelper = userHelper;
            NotificationRepository = notificationRepository;
        }
        public int NumberOfLinkedNotifications { get; set; }
        public int? LatestLinkedNotificationId { get; private set; }

        public Notification Notification { get; set; }
        public NotificationBannerModel NotificationBannerModel { get; set; }
        public IList<AlertWithTbServiceForDisplay> Alerts { get; set; }
        public DataQualityDraftAlert DraftAlert { get; set; }
        public PermissionLevel PermissionLevel { get; set; }
        public string PermissionReason { get; set; }
        public bool IsShared { get; set; }

        [BindProperty(SupportsGet = true)]
        public int NotificationId { get; set; }

        protected virtual async Task<Notification> GetNotificationAsync(int notificationId)
        {
            return await NotificationRepository.GetNotificationAsync(notificationId);
        }

        protected async Task AuthorizeAndSetBannerAsync()
        {
            var (permissionLevel, reason) = await _authorizationService.GetPermissionLevelAsync(User, Notification);
            PermissionLevel = permissionLevel;
            PermissionReason = reason;
            IsShared = PermissionLevel == PermissionLevel.SharedWith;
            NotificationBannerModel = new NotificationBannerModel(Notification, PermissionLevel == PermissionLevel.None);
        }

        protected async Task<bool> TryGetLinkedNotificationsAsync()
        {
            await GetLinkedNotificationsAsync();
            return Notification.Group != null;
        }

        protected async Task GetLinkedNotificationsAsync()
        {
            if (Notification.Group == null)
            {
                var notificationGroup = await GetNotificationGroupAsync();
                if (notificationGroup != null)
                {
                    notificationGroup.Notifications = notificationGroup.Notifications
                        .OrderBy(n => n.NotificationDate ?? n.CreationDate)
                        .ToList();
                    Notification.Group = notificationGroup;
                }
                NumberOfLinkedNotifications = Notification.Group?.Notifications.Count - 1 ?? 0;
                LatestLinkedNotificationId = Notification.Group?.Notifications.LastOrDefault()?.NotificationId;
            }
        }

        protected void PrepareBreadcrumbs()
        {
            var breadcrumbs = new List<Breadcrumb>
            {
                HttpContext.Session.GetTopLevelBreadcrumb(),
                new Breadcrumb {Label = "Notification", Url = $@"/Notifications/{NotificationId}"}
            };

            ViewData["Breadcrumbs"] = breadcrumbs;
        }

        protected IActionResult ForbiddenResult()
        {
            return StatusCode((int)HttpStatusCode.Forbidden);
        }

        public bool UserIsReadOnly()
        {
            return _userHelper.UserIsReadOnly(User);
        }

        private async Task<NotificationGroup> GetNotificationGroupAsync()
        {
            return await NotificationRepository.GetNotificationGroupAsync(NotificationId);
        }
    }
}
