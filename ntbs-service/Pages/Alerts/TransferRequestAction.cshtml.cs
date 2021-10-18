﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;
using ntbs_service.Pages.Notifications;
using ntbs_service.Services;

namespace ntbs_service.Pages.Alerts
{
    public class TransferRequestActionModel : NotificationModelBase
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IAlertService _alertService;
        private readonly IReferenceDataRepository _referenceDataRepository;
        private readonly ITreatmentEventRepository _treatmentEventRepository;

        public ValidationService ValidationService;

        [BindProperty]
        [Required(ErrorMessage = "Please accept or decline the transfer")]
        public bool? AcceptTransfer { get; set; }
        [BindProperty]
        [MaxLength(200)]
        [RegularExpression(ValidationRegexes.CharacterValidationAsciiBasic,
            ErrorMessage = ValidationMessages.InvalidCharacter)]
        [Display(Name = "Explanatory comment")]
        public string DeclineTransferReason { get; set; }

        [BindProperty]
        public int AlertId { get; set; }
        public TransferAlert TransferAlert { get; set; }
        public SelectList Hospitals { get; set; }
        public SelectList CaseManagers { get; set; }
        [BindProperty]
        public Guid TargetHospitalId { get; set; }
        [BindProperty]
        public int? TargetCaseManagerId { get; set; }


        public TransferRequestActionModel(
            INotificationService notificationService,
            IAlertService alertService,
            IAlertRepository alertRepository,
            IAuthorizationService authorizationService,
            INotificationRepository notificationRepository,
            IReferenceDataRepository referenceDataRepository,
            ITreatmentEventRepository treatmentEventRepository,
            IUserHelper userHelper)
            : base(notificationService, authorizationService, userHelper, notificationRepository)
        {
            _treatmentEventRepository = treatmentEventRepository;
            _alertService = alertService;
            _alertRepository = alertRepository;
            _referenceDataRepository = referenceDataRepository;
            ValidationService = new ValidationService(this);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Notification = await NotificationRepository.GetNotificationAsync(NotificationId);
            TransferAlert = await _alertRepository.GetOpenTransferAlertByNotificationId(NotificationId);
            await AuthorizeAndSetBannerAsync();

            // Check edit permission of user and redirect if not allowed
            if (!await _authorizationService.IsUserAuthorizedToManageAlert(User, TransferAlert))
            {
                return RedirectToPage("/Notifications/Overview", new { NotificationId });
            }

            if (TransferAlert == null)
            {
                return RedirectToPage("/Notifications/Overview", new { NotificationId });
            }

            var hospitals = await _referenceDataRepository.GetHospitalsByTbServiceCodesAsync(
                new List<string> { TransferAlert.TbServiceCode });
            Hospitals = new SelectList(hospitals,
                nameof(Hospital.HospitalId),
                nameof(Hospital.Name));
            var caseManagers = await _referenceDataRepository.GetActiveCaseManagersByTbServiceCodesAsync(
                new List<string> { TransferAlert.TbServiceCode });
            CaseManagers = new SelectList(caseManagers,
                nameof(Models.Entities.User.Id),
                nameof(Models.Entities.User.DisplayName));
            TargetCaseManagerId = TransferAlert.CaseManagerId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Notification = await NotificationRepository.GetNotificationAsync(NotificationId);
            TransferAlert = await _alertRepository.GetOpenTransferAlertByNotificationId(NotificationId);
            await AuthorizeAndSetBannerAsync();
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (AcceptTransfer == true)
            {
                await AcceptTransferAndDismissAlertAsync();
                return Partial("_AcceptedTransferConfirmation", this);
            }

            await RejectTransferAndDismissAlertAsync();
            return Partial("_RejectedTransferConfirmation", this);
        }

        public async Task AcceptTransferAndDismissAlertAsync()
        {
            var currentTime = DateTime.Now;
            var transferOutEvent = new TreatmentEvent
            {
                NotificationId = NotificationId,
                EventDate = currentTime,
                TreatmentEventType = TreatmentEventType.TransferOut,
                CaseManagerId = Notification.HospitalDetails.CaseManagerId,
                TbServiceCode = Notification.HospitalDetails.TBServiceCode,
                Note = TransferAlert.TransferRequestNote
            };
            var transferInEvent = new TreatmentEvent
            {
                NotificationId = NotificationId,
                EventDate = currentTime.AddSeconds(1),
                TreatmentEventType = TreatmentEventType.TransferIn,
                CaseManagerId = TransferAlert.CaseManagerId,
                TbServiceCode = TransferAlert.TbServiceCode,
                Note = TransferAlert.TransferRequestNote
            };

            var previousTbService = new PreviousTbService()
            {
                TransferDate = currentTime,
                TbServiceCode = Notification.HospitalDetails.TBServiceCode,
                PhecCode = Notification.HospitalDetails?.TBService?.PHECCode
            };

            Notification.PreviousTbServices.Add(previousTbService);
            Notification.HospitalDetails.TBServiceCode = TransferAlert.TbServiceCode;
            Notification.HospitalDetails.HospitalId = TargetHospitalId;
            Notification.HospitalDetails.CaseManagerId = TargetCaseManagerId;

            // Drop consultant and local patient ID because these are almost certainly going to be different at the new
            // TB service, as per NTBS-1559
            Notification.HospitalDetails.Consultant = null;
            Notification.PatientDetails.LocalPatientId = null;
            // We want to save all these changes in the same transaction so that we make sure the time between the first
            // and last audit logs are within the timeframe for them to be grouped together on the changes page. We
            // experienced an issue where the audits from an acceptance were over this threshold and our app didn't
            // know how to handle the group. See NTBS-2457
            Service.UpdatePatientDetailsWithoutSave(Notification, Notification.PatientDetails);
            Service.UpdateHospitalDetailsWithoutSave(Notification, Notification.HospitalDetails);
            _treatmentEventRepository.AddWithoutSave(transferOutEvent);
            _treatmentEventRepository.AddWithoutSave(transferInEvent);
            await _alertService.DismissAlertWithoutSaveAsync(TransferAlert.AlertId, User.Username());
            await _treatmentEventRepository.SaveChangesAsync(NotificationAuditType.Edited);
        }

        public async Task RejectTransferAndDismissAlertAsync()
        {
            var user = await _referenceDataRepository.GetUserByUsernameAsync(User.Username());
            var transferRejectedAlert = new TransferRejectedAlert
            {
                NotificationId = NotificationId,
                RejectionReason = DeclineTransferReason ?? "No reason was given when declining this transfer.",
                DecliningUserAndTbServiceString = $"{user.DisplayName}, {TransferAlert.TbService.Name}"
            };

            // Dismiss any existing transfer rejected alert so that the new one can be created
            var pendingTransferRejectedAlert =
                await _alertRepository.GetOpenAlertByNotificationId<TransferRejectedAlert>(NotificationId);
            if (pendingTransferRejectedAlert != null)
            {
                await _alertService.DismissAlertAsync(pendingTransferRejectedAlert.AlertId, User.Username());
            }
            await _alertService.AddUniqueOpenAlertAsync(transferRejectedAlert);
            await _alertService.DismissAlertAsync(TransferAlert.AlertId, User.Username());
        }
    }
}
