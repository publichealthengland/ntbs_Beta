using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using ntbs_service.DataAccess;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Services;
using Serilog;

namespace ntbs_service.Jobs
{
    public class DataQualityAlertsJob : HangfireJobContext
    {
        private readonly IAlertService _alertService;
        private readonly IDataQualityRepository _dataQualityRepository;
        private readonly NtbsContext _context;
        private const int CountPerBatch = 500;

        public DataQualityAlertsJob(
            IAlertService alertService,
            IDataQualityRepository dataQualityRepository,
            NtbsContext ntbsContext) : base(ntbsContext)
        {
            _alertService = alertService;
            _dataQualityRepository = dataQualityRepository;
            _context = ntbsContext;
        }

        private delegate Task<int> GetNotificationsEligibleForDqAlertsCount();
        private delegate Task<IList<Notification>> GetMultipleNotificationsEligibleForDataQualityAlerts(
            int countPerBatch,
            int offset);

        private async Task CreateDraftAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityDraftAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqDraftAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqDraftAlertsAsync
        );

        private async Task CreateBirthCountryAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityBirthCountryAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqBirthCountryAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqBirthCountryAlertsAsync
        );

        private async Task CreateClinicalDatesAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityClinicalDatesAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqClinicalDatesAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqClinicalDatesAlertsAsync
        );

        private async Task CreateDotVotAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityDotVotAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqDotVotAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqDotVotAlertsAsync
        );

        private async Task CreateTreatmentOutcome12MonthAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityTreatmentOutcome12>(
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome12AlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome12AlertsAsync
        );

        private async Task CreateTreatmentOutcome24MonthAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityTreatmentOutcome24>(
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome24AlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome24AlertsAsync
        );

        private async Task CreateTreatmentOutcome36MonthAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityTreatmentOutcome36>(
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome36AlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome36AlertsAsync
        );

        private async Task CreateChildECMLevelAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityChildECMLevel>(
            _dataQualityRepository.GetNotificationsEligibleForDqChildECMLevelAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqChildECMLevelAlertsAsync
        );

        private async Task CreateAlertsInBulkAsync<T>(
            GetNotificationsEligibleForDqAlertsCount getCount,
            GetMultipleNotificationsEligibleForDataQualityAlerts getNotifications) where T : Alert
        {
            var notificationsForAlertsCount = await getCount();
            var offset = 0;
            while (offset < notificationsForAlertsCount)
            {
                var notificationsForAlerts = await getNotifications(CountPerBatch, offset);

                var now = DateTime.Now;
                var dqAlerts = notificationsForAlerts
                    .Select(n =>
                    {
                        var alert = (T)Activator.CreateInstance(typeof(T));
                        alert.NotificationId = n.NotificationId;
                        alert.CreationDate = now;

                        return (Alert)alert;
                    })
                    .ToList();

                // When sourcing the alerts, we made sure to only take ones where no duplicate alerts will be created.
                // This means we are safe to just add them all and not have to ask _alertService to repeat that check
                await _alertService.AddAlertsRangeAsync(dqAlerts);
                offset += CountPerBatch;
            }
        }

        public async Task Run(IJobCancellationToken token)
        {
            Log.Information($"Starting data quality alerts job");
            await CreateDraftAlertsInBulkAsync();
            await CreateBirthCountryAlertsInBulkAsync();
            await CreateClinicalDatesAlertsInBulkAsync();
            await CreateDotVotAlertsInBulkAsync();
            await CreateTreatmentOutcome12MonthAlertsInBulkAsync();
            await CreateTreatmentOutcome24MonthAlertsInBulkAsync();
            await CreateTreatmentOutcome36MonthAlertsInBulkAsync();
            await CreateChildECMLevelAlertsInBulkAsync();

            // When we check for treatment outcome alerts above, quite a lot of notifications are being brought
            // into the context. (This is because the eligibility check cannot be entirely translated into SQL so
            // we are fetching more notifications that we really need from the database and processing them in
            // memory). Because Notifications have quite a lot of "owns" relationships, this is bringing quite a
            // large number of objects into the context. This in turn can make save operations slow. We have seen
            // that each duplicate alert save below was taking more than a second. Here we clear the change tracker
            // meaning that the context is able to handle saves much more quickly.
            _context.ChangeTracker.Clear();

            var possibleDuplicateNotificationIds =
                await _dataQualityRepository.GetNotificationIdsEligibleForDqPotentialDuplicateAlertsAsync();
            foreach (var notification in possibleDuplicateNotificationIds)
            {
                var alert = new DataQualityPotentialDuplicateAlert
                {
                    NotificationId = notification.NotificationId,
                    DuplicateId = notification.DuplicateId,
                    NhsNumberMismatch = notification.NhsNumber != notification.DuplicateNhsNumber
                                        && notification.NhsNumber != null && notification.DuplicateNhsNumber != null
                };
                await _alertService.AddUniquePotentialDuplicateAlertAsync(alert);
            }

            Log.Information($"Finished data quality alerts job");
        }
    }
}
