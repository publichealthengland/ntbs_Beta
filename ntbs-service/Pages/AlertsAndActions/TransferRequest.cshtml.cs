﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;
using ntbs_service.Models.FilteredSelectLists;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;
using ntbs_service.Models.ViewModels;
using ntbs_service.Pages.Notifications;
using ntbs_service.Services;

namespace ntbs_service.Pages.AlertsAndActions
{
    public class TransferRequestModel : NotificationModelBase
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IAlertService _alertService;
        private readonly IReferenceDataRepository _referenceDataRepository;
        public ValidationService ValidationService;

        public TransferAlert TransferAlert { get; set; }

        [BindProperty]
        public TransferRequestViewModel TransferRequest { get; set; }

        [BindProperty]
        public FormattedDate FormattedTransferDate { get; set; }

        [BindProperty]
        public int AlertId { get; set; }

        public SelectList TbServices { get; set; }
        public SelectList CaseManagers { get; set; }
        public SelectList Phecs { get; set; }

        public TransferRequestModel(
            INotificationService notificationService,
            IAlertService alertService,
            IAlertRepository alertRepository,
            IAuthorizationService authorizationService,
            INotificationRepository notificationRepository,
            IReferenceDataRepository referenceDataRepository,
            IUserHelper userHelper)
            : base(notificationService, authorizationService, userHelper, notificationRepository)
        {
            _alertService = alertService;
            _alertRepository = alertRepository;
            _referenceDataRepository = referenceDataRepository;
            ValidationService = new ValidationService(this);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Notification = await GetNotificationAsync(NotificationId);
            await AuthorizeAndSetBannerAsync();
            // Check edit permission and redirect if not allowed
            if (PermissionLevel != PermissionLevel.Edit)
            {
                return RedirectToPage("/Notifications/Overview", new { NotificationId });
            }

            var pendingTransferAlert =
                await _alertRepository.GetOpenTransferAlertByNotificationId(NotificationId);
            if (pendingTransferAlert != null)
            {
                TransferAlert = pendingTransferAlert;
                return Partial("_TransferPendingPartial", this);
            }

            TransferRequest = new TransferRequestViewModel { TransferDate = DateTime.Now.Date };
            SetDatesOnRequestForValidation(TransferRequest);
            FormattedTransferDate = TransferRequest.TransferDate.ConvertToFormattedDate();
            await SetDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Notification = await GetNotificationAsync(NotificationId);
            
            await AuthorizeAndSetBannerAsync();
            // Check edit permission and redirect if not allowed
            if (PermissionLevel != PermissionLevel.Edit)
            {
                return RedirectToPage("/Notifications/Overview", new { NotificationId });
            }
            
            await SetValuesForValidation();
            ModelState.Clear();
            TryValidateModel(TransferRequest, nameof(TransferRequest));
            if (!ModelState.IsValid)
            {
                await SetDropdownsAsync();
                await AuthorizeAndSetBannerAsync();
                return Page();
            }

            var transferAlert = new TransferAlert
            {
                NotificationId = NotificationId,
                TransferDate = TransferRequest.TransferDate,
                TbServiceCode = TransferRequest.TbServiceCode,
                CaseManagerId = TransferRequest.CaseManagerId,
                TransferReason = TransferRequest.TransferReason,
                OtherReasonDescription = TransferRequest.OtherReasonDescription,
                TransferRequestNote = TransferRequest.TransferRequestNote
            };
            await _alertService.AddUniqueOpenAlertAsync(transferAlert);

            return RedirectToPage("/Notifications/Overview", new { NotificationId });
        }

        private async Task SetValuesForValidation()
        {
            await GetRelatedEntities();
            ValidationService.TrySetFormattedDate(TransferRequest, "TransferRequest", nameof(TransferRequest.TransferDate), FormattedTransferDate);
            SetDatesOnRequestForValidation(TransferRequest);
        }

        private void SetDatesOnRequestForValidation(TransferViewModel transferRequest)
        {
            transferRequest.NotificationStartDate =
                NotificationHelper.Earliest(new[] {
                    Notification.ClinicalDetails.DiagnosisDate,
                    Notification.ClinicalDetails.TreatmentStartDate,
                    Notification.NotificationDate});
            transferRequest.LatestTransferDate = Notification.TreatmentEvents.OrderForEpisodes()
                .LastOrDefault(te => te.TreatmentEventType == TreatmentEventType.TransferIn)?.EventDate;
        }

        private async Task GetRelatedEntities()
        {
            if (TransferRequest.TbServiceCode != null)
            {
                TransferRequest.TbService =
                    await _referenceDataRepository.GetTbServiceByCodeAsync(TransferRequest.TbServiceCode);
            }

            if (TransferRequest.CaseManagerId.HasValue)
            {
                TransferRequest.CaseManager =
                    await _referenceDataRepository.GetUserByIdAsync(TransferRequest.CaseManagerId.Value);
            }

            TransferRequest.NotificationTbServiceCode = Notification.HospitalDetails.TBServiceCode;
        }

        private async Task SetDropdownsAsync()
        {
            var tbServices = await _referenceDataRepository.GetAllActiveTbServicesAsync();
            TbServices = new SelectList(tbServices, nameof(TBService.Code), nameof(TBService.Name));
            var caseManagers = await _referenceDataRepository.GetAllActiveCaseManagers();
            CaseManagers = new SelectList(caseManagers,
                nameof(Models.Entities.User.Id),
                nameof(Models.Entities.User.DisplayName));
            var phecs = await _referenceDataRepository.GetAllPhecs();
            Phecs = new SelectList(phecs, nameof(PHEC.Code), nameof(PHEC.Name));
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            Notification = await GetNotificationAsync(NotificationId);
            TransferAlert = await _alertRepository.GetOpenTransferAlertByNotificationId(NotificationId);
            await _alertService.DismissAlertAsync(TransferAlert.AlertId, User.Username());

            NotificationBannerModel = new NotificationBannerModel(Notification);
            return Partial("_CancelTransferConfirmation", this);
        }

        public async Task<JsonResult> OnGetFilteredTbServiceListsByPhecCode(string value)
        {
            var filteredTbServices = value != null
                ? await _referenceDataRepository.GetTbServicesFromPhecCodeAsync(value)
                : await _referenceDataRepository.GetAllActiveTbServicesAsync();

            return new JsonResult(
                new FilteredTransferPageSelectLists
                {
                    TbServices = filteredTbServices.Select(n => new OptionValue
                    {
                        Value = n.Code.ToString(),
                        Text = n.Name
                    })
                });
        }

        public async Task<JsonResult> OnGetFilteredCaseManagersListByTbServiceCode(string value)
        {
            var filteredCaseManagers =
                await _referenceDataRepository.GetActiveCaseManagersByTbServiceCodesAsync(new List<string> { value });

            return new JsonResult(
                new FilteredCaseManagerList
                {
                    CaseManagers = filteredCaseManagers.Select(n => new OptionValue
                    {
                        Value = n.Id.ToString(),
                        Text = n.DisplayName
                    })
                });
        }

        public async Task<ContentResult> OnPostValidateTransferRequestDate([FromBody] DateValidationModel validationData)
        {
            Notification = await GetNotificationAsync(NotificationId);
            var transferRequest = new TransferViewModel();
            SetDatesOnRequestForValidation(transferRequest);
            return ValidationService.GetDateValidationResult(transferRequest, validationData.Key, validationData.Day, validationData.Month, validationData.Year);
        }
    }
}
