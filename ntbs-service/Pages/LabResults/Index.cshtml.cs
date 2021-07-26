﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.Validations;
using ntbs_service.Services;
using IAuthorizationService = ntbs_service.Services.IAuthorizationService;

namespace ntbs_service.Pages.LabResults
{
    public class IndexModel : PageModel
    {
        public static readonly int ManualNotificationIdValue = -1;

        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ISpecimenService _specimenService;
        private readonly IUserService _userService;
        private readonly IUserHelper _userHelper;

        public IndexModel(
            ISpecimenService specimenService,
            IUserService userService,
            INotificationRepository notificationRepository,
            IAuthorizationService authorizationService,
            IUserHelper userHelper)
        {
            _specimenService = specimenService;
            _userService = userService;
            _notificationRepository = notificationRepository;
            _authorizationService = authorizationService;
            _userHelper = userHelper;
            ValidationService = new ValidationService(this);
        }

        public ValidationService ValidationService { get; }

        public IEnumerable<UnmatchedSpecimen> UnmatchedSpecimens { get; private set; }

        [BindProperty]
        public IDictionary<string, SpecimenPotentialMatchSelection> PotentialMatchSelections { get; set; } =
            new Dictionary<string, SpecimenPotentialMatchSelection>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (_userHelper.UserIsReadOnly(User))
            {
                return RedirectToPage(RouteHelper.AccessDeniedPath);
            }
            await PreparePageForGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PreparePageForGetAsync();
                return Page();
            }

            var (laboratoryReferenceNumber, value) = PotentialMatchSelections.Single();
            int notificationId;

            if (value.NotificationIdIsManual)
            {
                notificationId = await ValidateManualMatch(value);
            }
            else
            {
                notificationId = await ValidateCandidateMatch(value);
            }

            if (!ModelState.IsValid)
            {
                await PreparePageForGetAsync();
                return Page();
            }

            await FetchUnmatchedSpecimensAsync();
            if (UnmatchedSpecimens.All(specimen => specimen.ReferenceLaboratoryNumber != laboratoryReferenceNumber))
            {
                throw new DataException(
                    "When performing a specimen match via `/LabResults`, the chosen specimen reference " +
                    "number was either previously matched, or unavailable to the user.");
            }

            var userName = User.Username();
            await _specimenService.MatchSpecimenAsync(notificationId, laboratoryReferenceNumber, userName);
            AddTempDataForSuccessfulMessage(notificationId, laboratoryReferenceNumber);

            // Explicit path to the current page to avoid persisting any 'scroll to id' path values
            return RedirectToPage("/LabResults/Index");
        }

        public async Task<IActionResult> OnGetPotentialMatchDataAsync(string manualNotificationId)
        {
            var dynamicModel = new SpecimenPotentialMatchSelection { ManualNotificationId = manualNotificationId };

            const string dynamicValidationModelName = "DynamicModel";
            const string propertyName = nameof(SpecimenPotentialMatchSelection.ManualNotificationId);
            var combinedModelPropertyKey = $"{dynamicValidationModelName}.{propertyName}";
            ValidationService.ValidateProperty(dynamicModel, dynamicValidationModelName, manualNotificationId,
                propertyName);

            if (!ValidationService.IsValid(combinedModelPropertyKey))
            {
                var errorMessage = ModelState[combinedModelPropertyKey].Errors.First().ErrorMessage;
                return new JsonResult(new { errorMessage });
            }

            if (string.IsNullOrWhiteSpace(manualNotificationId))
            {
                return new JsonResult(new {});
            }

            var parsedManualNotificationId = int.Parse(manualNotificationId);
            var notification = await _notificationRepository.GetNotifiedNotificationAsync(parsedManualNotificationId);
            if (notification == null)
            {
                return new JsonResult(new { errorMessage = ValidationMessages.LabResultNotificationDoesNotExist });
            }

            var (permissionLevel, _) = await _authorizationService.GetPermissionLevelAsync(User, notification);
            if (permissionLevel != PermissionLevel.Edit)
            {
                return new JsonResult(new { errorMessage = ValidationMessages.LabResultNotificationMatchNoPermission });
            }

            var potentialMatch = MapNotificationToSpecimenPotentialMatch(notification);

            return new PartialViewResult
            {
                ViewName = "_labResultsDemographics",
                ViewData = new ViewDataDictionary<SpecimenPotentialMatch>(ViewData, potentialMatch)
            };
        }

        private async Task PreparePageForGetAsync()
        {
            await FetchUnmatchedSpecimensAsync();

            foreach (var unmatchedSpecimen in UnmatchedSpecimens)
            {
                PotentialMatchSelections.TryAdd(unmatchedSpecimen.ReferenceLaboratoryNumber, null);
            }

            ViewData["EditPageErrorDictionary"] = EditPageValidationErrorGenerator.MapToDictionary(ModelState);
        }

        private async Task FetchUnmatchedSpecimensAsync()
        {
            var permissionsFilter = await _userService.GetUserPermissionsFilterAsync(User);
            if (permissionsFilter.Type == UserType.NationalTeam)
            {
                UnmatchedSpecimens = await _specimenService.GetAllUnmatchedSpecimensAsync();
            }
            else if (permissionsFilter.FilterByTBService)
            {
                UnmatchedSpecimens = await _specimenService.GetUnmatchedSpecimensDetailsForTbServicesAsync(
                    permissionsFilter.IncludedTBServiceCodes);
            }
            else if (permissionsFilter.FilterByPHEC)
            {
                UnmatchedSpecimens = await _specimenService.GetUnmatchedSpecimensDetailsForPhecsAsync(
                    permissionsFilter.IncludedPHECCodes);
            }
        }

        private void AddTempDataForSuccessfulMessage(int notificationId, string laboratoryReferenceNumber)
        {
            TempData["HasSuccessfulMatch"] = true;
            TempData["MatchedReferenceNumber"] = laboratoryReferenceNumber;
            TempData["MatchedNotificationId"] = notificationId;
        }

        private async Task<int> ValidateCandidateMatch(SpecimenPotentialMatchSelection selection)
        {
            // Prefix ModelState endsWith substring with '.' to avoid catching x.ManualNotificationId
            var candidateMatchModelStateKey =
                ModelState.Keys.SingleOrDefault(key =>
                    key.EndsWith("." + nameof(SpecimenPotentialMatchSelection.NotificationId)));

            var notificationId = selection.NotificationId.GetValueOrDefault();
            var notification = await _notificationRepository.GetNotifiedNotificationAsync(notificationId);

            if (notification == null)
            {
                throw new DataException(
                    "When performing a specimen match via `/LabResults`, a candidate match was not found in the ntbs database.");
            }

            var (permissionLevel, _) = await _authorizationService.GetPermissionLevelAsync(User, notification);
            if (permissionLevel != PermissionLevel.Edit)
            {
                ModelState.AddModelError(candidateMatchModelStateKey,
                    ValidationMessages.LabResultNotificationMatchNoPermission);
            }

            return notificationId;
        }

        private async Task<int> ValidateManualMatch(SpecimenPotentialMatchSelection value)
        {
            var manualMatchModelStateKey =
                ModelState.Keys.SingleOrDefault(key =>
                    key.EndsWith(nameof(SpecimenPotentialMatchSelection.ManualNotificationId)));

            var notificationId = int.Parse(value.ManualNotificationId);
            var notification = await _notificationRepository.GetNotifiedNotificationAsync(notificationId);

            if (notification == null)
            {
                ModelState.AddModelError(manualMatchModelStateKey,
                    ValidationMessages.LabResultNotificationDoesNotExist);
            }
            else
            {
                var (permissionLevel, _) = await _authorizationService.GetPermissionLevelAsync(User, notification);
                if (permissionLevel != PermissionLevel.Edit)
                {
                    ModelState.AddModelError(manualMatchModelStateKey,
                        ValidationMessages.LabResultNotificationMatchNoPermission);
                }
            }

            return notificationId;
        }

        private static SpecimenPotentialMatch MapNotificationToSpecimenPotentialMatch(Notification notification)
        {
            return new SpecimenPotentialMatch
            {
                NotificationId = notification.NotificationId,
                NotificationDate = notification.NotificationDate,
                NtbsAddress = notification.PatientDetails.Address,
                NtbsName = notification.PatientDetails.FullName,
                NtbsPostcode = notification.PatientDetails.Postcode,
                NtbsBirthDate = notification.PatientDetails.Dob,
                NtbsNhsNumber = notification.PatientDetails.FormattedNhsNumber,
                NtbsSex = notification.PatientDetails.SexLabel,
                TbServiceName = notification.HospitalDetails.TBServiceName
            };
        }
    }
}
