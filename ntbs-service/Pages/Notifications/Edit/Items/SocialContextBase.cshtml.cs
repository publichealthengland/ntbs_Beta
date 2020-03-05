using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Services;

namespace ntbs_service.Pages.Notifications.Edit.Items
{
    public abstract class SocialContextBaseModel<T> : NotificationEditModelBase where T : SocialContextBase
    {
        protected readonly IItemRepository<T> _socialContextRepository;
        private readonly IAlertService _alertService;

        [BindProperty(SupportsGet = true)]
        public int? RowId { get; set; }

        [BindProperty]
        public FormattedDate FormattedDateFrom { get; set; }
        [BindProperty]
        public FormattedDate FormattedDateTo { get; set; }

        protected SocialContextBaseModel(
            INotificationService service,
            IAuthorizationService authorizationService,
            INotificationRepository notificationRepository,
            IItemRepository<T> socialContextRepository,
            IAlertService alertService,
            IAlertRepository alertRepository)
            : base(service, authorizationService, notificationRepository, alertRepository)
        {
            _socialContextRepository = socialContextRepository;
            _alertService = alertService;
        }

        protected void FormatDatesForGet(T model)
        {
            FormattedDateFrom = model.DateFrom.ConvertToFormattedDate();
            FormattedDateTo = model.DateTo.ConvertToFormattedDate();
        }

        protected async Task ValidateAndSave(T model, string modelName)
        {
            model.NotificationId = NotificationId;
            model.Dob = Notification.PatientDetails.Dob;
            SetDates(model, modelName);
            model.SetValidationContext(Notification);

            if (TryValidateModel(model, modelName))
            {
                if (RowId == null)
                {
                    await _socialContextRepository.AddAsync(model);
                    await _alertService.AutoDismissAlertAsync<DataQualityClusterAlert>(Notification);
                }
                else
                {
                    model.Id = RowId.Value;
                    await _socialContextRepository.UpdateAsync(Notification, model);
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            Notification = await GetNotificationAsync(NotificationId);
            if (await AuthorizationService.GetPermissionLevelForNotificationAsync(User, Notification) != PermissionLevel.Edit)
            {
                return ForbiddenResult();
            }

            var model = GetSocialContextBaseById(Notification, RowId.Value);
            if (model == null)
            {
                return NotFound();
            }

            await _socialContextRepository.DeleteAsync(model);

            return RedirectForNotified();
        }

        private void SetDates(T model, string modelName)
        {
            // The required date will be marked as missing on the model, since we are setting it manually, rather than binding it
            ModelState.Remove($"{modelName}.DateFrom");
            ValidationService.TrySetFormattedDate(model, modelName, nameof(model.DateFrom), FormattedDateFrom);
            ModelState.Remove($"{modelName}.DateTo");
            ValidationService.TrySetFormattedDate(model, modelName, nameof(model.DateTo), FormattedDateTo);
        }

        public ContentResult OnGetValidateSocialContextProperty(string key, string value, bool shouldValidateFull)
        {
            return ValidationService.GetPropertyValidationResult<SocialContextVenue>(key, value, shouldValidateFull);
        }

        public async Task<ContentResult> OnGetValidateSocialContextDate(string key, string day, string month, string year)
        {
            var isLegacy = await NotificationRepository.IsNotificationLegacyAsync(NotificationId);
            return ValidationService.GetDateValidationResult<T>(key, day, month, year, isLegacy);
        }

        public ContentResult OnGetValidateSocialContextDates(IEnumerable<Dictionary<string, string>> keyValuePairs)
        {
            List<(string, object)> propertyValueTuples = new List<(string key, object property)>();
            foreach (var keyValuePair in keyValuePairs)
            {
                var formattedDate = new FormattedDate() { Day = keyValuePair["day"], Month = keyValuePair["month"], Year = keyValuePair["year"] };
                if (formattedDate.TryConvertToDateTime(out DateTime? convertedDob))
                {
                    propertyValueTuples.Add((keyValuePair["key"], convertedDob));
                }
                else
                {
                    // should not ever get here as we validate individual dates first before comparing
                    return null;
                }
            }
            return ValidationService.GetMultiplePropertiesValidationResult<T>(propertyValueTuples);
        }

        protected abstract T GetSocialContextBaseById(Notification notification, int id);
    }
}
