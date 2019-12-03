﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Services;

namespace ntbs_service.Pages.Notifications.Edit
{
    public class SocialRiskFactorsModel : NotificationEditModelBase
    {
        [BindProperty]
        public SocialRiskFactors SocialRiskFactors { get; set; }

        public SocialRiskFactorsModel(
            INotificationService service,
            IAuthorizationService authorizationService,
            IAlertRepository alertRepository,
            INotificationRepository notificationRepository) : base(service, authorizationService, alertRepository, notificationRepository)
        {
        }

        protected override async Task<IActionResult> PreparePageForGet(int id, bool isBeingSubmitted)
        {
            SocialRiskFactors = Notification.SocialRiskFactors;
            await SetNotificationProperties(isBeingSubmitted, SocialRiskFactors);

            return Page();
        }

        protected override async Task ValidateAndSave()
        {
            SocialRiskFactors.SetFullValidation(Notification.NotificationStatus);
            if (TryValidateModel(SocialRiskFactors))
            {
                await Service.UpdateSocialRiskFactorsAsync(Notification, SocialRiskFactors);
            }
        }

        protected override IActionResult RedirectToNextPage(int notificationId, bool isBeingSubmitted)
        {
            return RedirectToPage("./Travel", new { id = notificationId, isBeingSubmitted });
        }
    }
}
