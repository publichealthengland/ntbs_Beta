﻿using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using ntbs_service.DataAccess;
using ntbs_service.Services;
using Serilog;

namespace ntbs_service.Jobs
{
    public class UnmatchedLabResultAlertsJob : HangfireJobContext
    {
        private readonly ISpecimenService _specimenService;
        private readonly IAlertRepository _alertRepository;
        private readonly IAlertService _alertService;

        public UnmatchedLabResultAlertsJob(
            ISpecimenService specimenService,
            IAlertRepository alertRepository,
            IAlertService alertService,
            NtbsContext ntbsContext) : base(ntbsContext)
        {
            _specimenService = specimenService;
            _alertRepository = alertRepository;
            _alertService = alertService;
        }

        public async Task Run(IJobCancellationToken token)
        {
            Log.Information($"Starting unmatched lab results alert job");

            var dbPotentialMatches =
                await _specimenService.GetAllSpecimenPotentialMatchesAsync();

            var currentOpenUnmatchedLabResultAlerts =
                await _alertRepository.GetAllOpenUnmatchedLabResultAlertsAsync();

            var unneededAlerts = currentOpenUnmatchedLabResultAlerts.Where(
                alert => !dbPotentialMatches.Any(dbMatch =>
                    dbMatch.NotificationId == alert.NotificationId &&
                    dbMatch.ReferenceLaboratoryNumber == alert.SpecimenId)).ToList();

            Log.Debug($"Number of redundant unmatched lab result alerts to be closed: {unneededAlerts.Count}");
            await _alertRepository.CloseAlertRangeAsync(unneededAlerts);

            var dbMatchesWithoutOpenAlert = dbPotentialMatches.Where(
                dbMatch => !currentOpenUnmatchedLabResultAlerts.Any(alert =>
                    alert.NotificationId == dbMatch.NotificationId &&
                    alert.SpecimenId == dbMatch.ReferenceLaboratoryNumber)).ToList();

            Log.Debug($"Number of unmatched lab results without an open alert: {dbMatchesWithoutOpenAlert.Count}");
            await _alertService.CreateAlertsForUnmatchedLabResults(dbMatchesWithoutOpenAlert);

            Log.Information($"Finishing unmatched lab results alert job");
        }
    }
}
