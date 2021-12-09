﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;
using ntbs_service.Services;

namespace ntbs_service.Pages.Notifications.Edit.Items
{
    public class MBovisUnpasteurisedMilkConsumptionModel : NotificationEditModelBase
    {
        private readonly IItemRepository<MBovisUnpasteurisedMilkConsumption> _mBovisUnpasteurisedMilkConsumptionRepository;
        private readonly IReferenceDataRepository _referenceDataRepository;

        [BindProperty(SupportsGet = true)]
        public int? RowId { get; set; }

        [BindProperty]
        public MBovisUnpasteurisedMilkConsumption MBovisUnpasteurisedMilkConsumption { get; set; }

        public SelectList Countries { get; set; }

        public MBovisUnpasteurisedMilkConsumptionModel(
            INotificationService service,
            IAuthorizationService authorizationService,
            INotificationRepository notificationRepository,
            IReferenceDataRepository referenceDataRepository,
            IItemRepository<MBovisUnpasteurisedMilkConsumption> mBovisUnpasteurisedMilkConsumptionRepository,
            IAlertRepository alertRepository,
            IUserHelper userHelper) : base(service, authorizationService, notificationRepository, alertRepository, userHelper)
        {
            _referenceDataRepository = referenceDataRepository;
            _mBovisUnpasteurisedMilkConsumptionRepository = mBovisUnpasteurisedMilkConsumptionRepository;

            Countries = new SelectList(
                _referenceDataRepository.GetAllCountriesAsync().Result,
                nameof(Country.CountryId),
                nameof(Country.Name));
        }

        // Pragma disabled 'not using async' to allow auto-magical wrapping in a Task
#pragma warning disable 1998
        protected override async Task<IActionResult> PrepareAndDisplayPageAsync(bool isBeingSubmitted)
#pragma warning restore 1998
        {
            if (!Notification.IsMBovis)
            {
                return NotFound();
            }

            if (RowId == null)
            {
                return Page();
            }

            MBovisUnpasteurisedMilkConsumption = Notification.MBovisDetails.MBovisUnpasteurisedMilkConsumptions
                .SingleOrDefault(m => m.MBovisUnpasteurisedMilkConsumptionId == RowId);
            if (MBovisUnpasteurisedMilkConsumption == null)
            {
                return NotFound();
            }

            return Page();
        }

        protected override async Task ValidateAndSave()
        {
            MBovisUnpasteurisedMilkConsumption.SetValidationContext(Notification);
            MBovisUnpasteurisedMilkConsumption.NotificationId = NotificationId;
            MBovisUnpasteurisedMilkConsumption.DobYear = Notification.PatientDetails.Dob?.Year;

            if (TryValidateModel(MBovisUnpasteurisedMilkConsumption, nameof(MBovisUnpasteurisedMilkConsumption)))
            {
                if (RowId == null)
                {
                    await _mBovisUnpasteurisedMilkConsumptionRepository.AddAsync(MBovisUnpasteurisedMilkConsumption);
                }
                else
                {
                    MBovisUnpasteurisedMilkConsumption.MBovisUnpasteurisedMilkConsumptionId = RowId.Value;
                    await _mBovisUnpasteurisedMilkConsumptionRepository.UpdateAsync(Notification,
                        MBovisUnpasteurisedMilkConsumption);
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            Notification = await GetNotificationAsync(NotificationId);
            var (permissionLevel, _) = await _authorizationService.GetPermissionLevelAsync(User, Notification);
            if (permissionLevel != Models.Enums.PermissionLevel.Edit)
            {
                return ForbiddenResult();
            }

            var mBovisUnpasteurisedMilkConsumption = Notification.MBovisDetails.MBovisUnpasteurisedMilkConsumptions
                .SingleOrDefault(m => m.MBovisUnpasteurisedMilkConsumptionId == RowId);
            if (mBovisUnpasteurisedMilkConsumption == null)
            {
                return NotFound();
            }

            await _mBovisUnpasteurisedMilkConsumptionRepository.DeleteAsync(mBovisUnpasteurisedMilkConsumption);

            return RedirectToPage("/Notifications/Edit/MBovisUnpasteurisedMilkConsumptions", new { NotificationId });
        }

        public ContentResult OnPostValidateMBovisUnpasteurisedMilkConsumptionProperty([FromBody]InputValidationModel validationData)
        {
            return ValidationService.GetPropertyValidationResult<MBovisUnpasteurisedMilkConsumption>(validationData.Key, validationData.Value, validationData.ShouldValidateFull);
        }

        protected override IActionResult RedirectForNotified()
        {
            return RedirectToPage("/Notifications/Edit/MBovisUnpasteurisedMilkConsumptions", new { NotificationId });
        }

        protected override IActionResult RedirectForDraft(bool isBeingSubmitted)
        {
            return RedirectToPage("/Notifications/Edit/MBovisUnpasteurisedMilkConsumptions", new { NotificationId });
        }

        protected override async Task<Notification> GetNotificationAsync(int notificationId)
        {
            return await NotificationRepository.GetNotificationWithMBovisUnpasteurisedMilkConsumptionAsync(notificationId);
        }
    }
}
