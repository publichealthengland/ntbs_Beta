﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using ntbs_service.DataAccess;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;

namespace ntbs_service.Services
{
    public interface IAlertService
    {
        Task<bool> AddUniqueAlertAsync<T>(T alert) where T : Alert;
        Task AddAlertsRangeAsync(List<Alert> alerts);
        Task AddUniquePotentialDuplicateAlertAsync(DataQualityPotentialDuplicateAlert alert);
        Task<bool> AddUniqueOpenAlertAsync<T>(T alert) where T : Alert;
        Task DismissAlertAsync(int alertId, string userId);
        Task DismissAlertWithoutSaveAsync(int alertId, string userId);
        Task AutoDismissAlertAsync<T>(Notification notification) where T : Alert;
        Task DismissMatchingAlertAsync<T>(int notificationId, string auditUsername = AuditService.AuditUserSystem) where T : Alert;
        Task DismissAllOpenAlertsForNotification(int notificationId);
        Task<IList<AlertWithTbServiceForDisplay>> GetAlertsForNotificationAsync(int notificationId, ClaimsPrincipal user);
        Task CreateAlertsForUnmatchedLabResults(IEnumerable<SpecimenMatchPairing> specimenMatchPairings);
    }

    public class AlertService : IAlertService
    {
        private readonly IAlertRepository _alertRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IAuthorizationService _authorizationService;

        public AlertService(
            IAlertRepository alertRepository,
            INotificationRepository notificationRepository,
            IAuthorizationService authorizationService)
        {
            _alertRepository = alertRepository;
            _notificationRepository = notificationRepository;
            _authorizationService = authorizationService;
        }

        public async Task DismissAlertAsync(int alertId, string userId)
        {
            var alert = await _alertRepository.GetOpenAlertByIdAsync(alertId);

            if (alert != null)
            {
                alert.ClosingUserId = userId;
                alert.ClosureDate = DateTime.Now;
                alert.AlertStatus = AlertStatus.Closed;

                await _alertRepository.SaveAlertChangesAsync();
            }
        }

        public async Task DismissAlertWithoutSaveAsync(int alertId, string userId)
        {
            var alert = await _alertRepository.GetOpenAlertByIdAsync(alertId);

            if (alert != null)
            {
                alert.ClosingUserId = userId;
                alert.ClosureDate = DateTime.Now;
                alert.AlertStatus = AlertStatus.Closed;
            }
        }

        public async Task AutoDismissAlertAsync<T>(Notification notification) where T : Alert
        {
            var alert = await _alertRepository.GetOpenAlertByNotificationId<T>(notification.NotificationId);
            if (alert == null) { return; }

            Func<Notification, bool> notificationQualifiesCheck;
            switch (alert)
            {
                case DataQualityBirthCountryAlert _:
                    notificationQualifiesCheck = DataQualityBirthCountryAlert.NotificationQualifies;
                    break;
                case DataQualityDraftAlert _:
                    notificationQualifiesCheck = DataQualityDraftAlert.NotificationQualifies;
                    break;
                case DataQualityClinicalDatesAlert _:
                    notificationQualifiesCheck = DataQualityClinicalDatesAlert.NotificationQualifies;
                    break;
                case DataQualityClusterAlert _:
                    notificationQualifiesCheck = DataQualityClusterAlert.NotificationQualifies;
                    break;
                case DataQualityTreatmentOutcome12 _:
                    notificationQualifiesCheck = DataQualityTreatmentOutcome12.NotificationQualifies;
                    break;
                case DataQualityTreatmentOutcome24 _:
                    notificationQualifiesCheck = DataQualityTreatmentOutcome24.NotificationQualifies;
                    break;
                case DataQualityTreatmentOutcome36 _:
                    notificationQualifiesCheck = DataQualityTreatmentOutcome36.NotificationQualifies;
                    break;
                case DataQualityDotVotAlert _:
                    notificationQualifiesCheck = DataQualityDotVotAlert.NotificationQualifies;
                    break;
                case DataQualityChildECMLevel _:
                    notificationQualifiesCheck = DataQualityChildECMLevel.NotificationQualifies;
                    break;
                default: throw new ArgumentException("Unexpected alert type passed for automatic closing");
            }

            if (!notificationQualifiesCheck(notification))
            {
                await DismissAlertAsync(alert.AlertId, null);
            }
        }

        public async Task DismissAllOpenAlertsForNotification(int notificationId)
        {
            var alerts = await _alertRepository.GetAllOpenAlertsByNotificationId(notificationId);

            foreach (var alert in alerts)
            {
                // notification may have multiple duplicate alerts to be dismissed
                if (alert is DataQualityPotentialDuplicateAlert duplicateAlert)
                {
                    await DismissTwinnedRecordDuplicationAlerts(notificationId, duplicateAlert);
                }

                SetAlertClosureDateAndAlertStatus(alert);
            }

            await _alertRepository.SaveAlertChangesAsync();
        }

        private async Task DismissTwinnedRecordDuplicationAlerts(int notificationId, DataQualityPotentialDuplicateAlert duplicateAlert)
        {
            var twinRecordAlerts = await _alertRepository.GetAllOpenAlertsByNotificationId(duplicateAlert.DuplicateId);
            if (twinRecordAlerts != null)
            {
                foreach (var twinRecordAlert in twinRecordAlerts)
                {
                    if (twinRecordAlert is DataQualityPotentialDuplicateAlert twinRecordDuplicateAlert &&
                        // Only want to dismiss duplicate alerts where the duplicateId is equal to the original notification being denotified
                        twinRecordDuplicateAlert.DuplicateId == notificationId)
                    {
                        SetAlertClosureDateAndAlertStatus(twinRecordDuplicateAlert);
                    }
                }
            }
        }

        private static void SetAlertClosureDateAndAlertStatus(Alert alert)
        {
            alert.ClosureDate = DateTime.Now;
            alert.AlertStatus = AlertStatus.Closed;
        }

        public async Task<bool> AddUniqueAlertAsync<T>(T alert) where T : Alert
        {
            if (alert.NotificationId.HasValue)
            {
                var matchingAlert =
                    await _alertRepository.GetAlertByNotificationIdAndTypeAsync<T>(alert.NotificationId.Value);
                if (matchingAlert != null)
                {
                    return false;
                }
            }

            await PopulateAndAddAlertAsync(alert);
            return true;
        }

        public async Task AddAlertsRangeAsync(List<Alert> alerts)
        {
            await _alertRepository.AddAlertRangeAsync(alerts);
        }

        public async Task<bool> AddUniqueOpenAlertAsync<T>(T alert) where T : Alert
        {
            if (alert.NotificationId.HasValue)
            {
                var matchingAlert = await _alertRepository.GetOpenAlertByNotificationId<T>(alert.NotificationId.Value);
                if (matchingAlert != null)
                {
                    return false;
                }
            }

            await PopulateAndAddAlertAsync(alert);
            return true;
        }

        public async Task AddUniquePotentialDuplicateAlertAsync(DataQualityPotentialDuplicateAlert alert)
        {
            if (alert.NotificationId.HasValue)
            {
                var matchingAlert = await _alertRepository.GetDuplicateAlertByNotificationIdAndDuplicateId(alert.NotificationId.Value, alert.DuplicateId);
                if (matchingAlert != null)
                {
                    return;
                }
            }

            await PopulateAndAddAlertAsync(alert);
        }

        public async Task DismissMatchingAlertAsync<T>(int notificationId, string auditUsername) where T : Alert
        {
            var matchingAlert = await _alertRepository.GetAlertByNotificationIdAndTypeAsync<T>(notificationId);
            if (matchingAlert != null)
            {
                await DismissAlertAsync(matchingAlert.AlertId, auditUsername);
            }
        }

        public async Task<IList<AlertWithTbServiceForDisplay>> GetAlertsForNotificationAsync(int notificationId, ClaimsPrincipal user)
        {
            var alerts = await _alertRepository.GetOpenAlertsForNotificationAsync(notificationId);
            var filteredAlerts = await _authorizationService.FilterAlertsForUserAsync(user, alerts);

            return filteredAlerts;
        }

        public async Task CreateAlertsForUnmatchedLabResults(IEnumerable<SpecimenMatchPairing> specimenMatchPairings)
        {
            foreach (var specimenMatchPairing in specimenMatchPairings)
            {
                var notification = await _notificationRepository.GetNotificationForAlertCreationAsync(
                    specimenMatchPairing.NotificationId);

                if (notification == null)
                {
                    throw new DataException(
                        $"Reporting database sourced NotificationId {specimenMatchPairing.NotificationId} was not found in NTBS database.");
                }

                var alert = new UnmatchedLabResultAlert
                {
                    AlertStatus = AlertStatus.Open,
                    NotificationId = notification.NotificationId,
                    SpecimenId = specimenMatchPairing.ReferenceLaboratoryNumber
                };

                var existingAlert = await _alertRepository.GetUnmatchedLabResultAlertForNotificationAndSpecimenAsync(
                    alert.NotificationId.Value,
                    alert.SpecimenId);

                if (existingAlert == null)
                {
                    await PopulateAndAddAlertAsync(alert);
                }
            }
        }

        private async Task PopulateAndAddAlertAsync(Alert alert)
        {
            alert.CreationDate = DateTime.Now;
            await _alertRepository.AddAlertAsync(alert);
        }
    }
}
