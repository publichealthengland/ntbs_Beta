using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Pages;
using ntbs_service.Services;

namespace ntbs_service.Pages_Notifications
{
    public class PatientModel : NotificationModelBase
    {
        private readonly NtbsContext context;

        public SelectList Ethnicities { get; set; }
        public SelectList Countries { get; set; }
        public List<Sex> Sexes { get; set; }

        [BindProperty]
        public PatientDetails Patient { get; set; }

        [BindProperty]
        public FormattedDate FormattedDob { get; set; }
        [BindProperty]
        public int NotificationId { get; set; }
        [BindProperty]
        public NotificationBannerDetails NotificationBannerDetails { get; set; }

        public PatientModel(INotificationService service, NtbsContext context) : base(service)
        {
            this.context = context;
        }

        public override async Task<IActionResult> OnGetAsync(int? id)
        {
            var notification = await service.GetNotificationAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            NotificationId = notification.NotificationId;
            Patient = notification.PatientDetails;
            var postcodeNoWhiteSpace = notification.PatientDetails.Postcode.Replace(" ", string.Empty);
            var formattedPostcode = postcodeNoWhiteSpace.Insert(postcodeNoWhiteSpace.Length - 3, " ");
            NotificationBannerDetails = new NotificationBannerDetails {
                NotificationId = NotificationId,
                Patient = notification.PatientDetails,
                Episode = notification.Episode,
                Postcode = formattedPostcode
            };

            if (Patient == null)
            {
                Patient = new PatientDetails();
            }

            FormattedDob = Patient.Dob.ConvertToFormattedDate();
            Ethnicities = new SelectList(context.GetAllEthnicitiesAsync().Result, nameof(Ethnicity.EthnicityId), nameof(Ethnicity.Label));
            Countries = new SelectList(context.GetAllCountriesAsync().Result, nameof(Country.CountryId), nameof(Country.Name));
            Sexes = context.GetAllSexesAsync().Result.ToList();

            return Page();
        }

        protected override async Task<bool> ValidateAndSave(int? NotificationId) {
            UpdatePatientFlags();
            SetAndValidateDateOnModel(Patient, nameof(Patient.Dob), FormattedDob);
            
            if (!ModelState.IsValid)
            {
                return false;
            }

            var notification = await service.GetNotificationAsync(NotificationId);
            await service.UpdatePatientAsync(notification, Patient);
            return true;
        }

        private void UpdatePatientFlags() {
            if (Patient.NhsNumberNotKnown) {
                Patient.NhsNumber = null;
                ModelState.Remove("Patient.NhsNumber");
            }

            if (Patient.NoFixedAbode) {
                Patient.Postcode = null;
                ModelState.Remove("Patient.Postcode");
            }
        }

        protected override IActionResult RedirectToNextPage(int? notificationId) {
            return RedirectToPage("./Episode", new {id = notificationId});
        }

        public ContentResult OnGetValidatePatientProperty(string key, string value)
        {
            return ValidateProperty(new PatientDetails(), key, value);
        }

        public ContentResult OnGetValidatePatientDate(string key, string day, string month, string year)
        {
            return ValidateDate(new PatientDetails(), key, day, month, year);
        }
    }
}