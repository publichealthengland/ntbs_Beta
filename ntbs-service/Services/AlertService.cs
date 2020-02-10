﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ntbs_service.DataAccess;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;

namespace ntbs_service.Services
{
    public interface IAlertService
    {
        Task<bool> AddUniqueAlertAsync(Alert alert);
        Task<bool> AddUniqueOpenAlertAsync(Alert alert);
        Task DismissAlertAsync(int alertId, string userId);
        Task DismissMatchingAlertAsync(int notificationId, AlertType alertType);
        Task<IList<Alert>> GetAlertsForNotificationAsync(int notificationId, ClaimsPrincipal user);
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
            var alert = await _alertRepository.GetAlertByIdAsync(alertId);

            alert.ClosingUserId = userId;
            alert.ClosureDate = DateTime.Now;
            alert.AlertStatus = AlertStatus.Closed;

            await _alertRepository.SaveAlertChangesAsync();
        }

        public async Task<bool> AddUniqueAlertAsync(Alert alert)
        {
            if (alert.NotificationId.HasValue)
            {
                var matchingAlert =
                    await _alertRepository.GetAlertByNotificationIdAndTypeAsync(alert.NotificationId.Value,
                        alert.AlertType);
                if (matchingAlert != null)
                {
                    return false;
                }
            }

            await PopulateAndAddAlertAsync(alert);
            return true;
        }

        public async Task<bool> AddUniqueOpenAlertAsync(Alert alert)
        {
            if (alert.NotificationId.HasValue)
            {
                var matchingAlert =
                    await _alertRepository.GetOpenAlertByNotificationIdAndTypeAsync(alert.NotificationId.Value,
                        alert.AlertType);
                if (matchingAlert != null)
                {
                    return false;
                }
            }

            await PopulateAndAddAlertAsync(alert);
            return true;
        }

        public async Task PopulateAndAddAlertAsync(Alert alert)
        {
            if (alert.NotificationId != null)
            {
                var notification = await _notificationRepository.GetNotificationAsync(alert.NotificationId.Value);
                alert.CreationDate = DateTime.Now;
                if (alert.CaseManagerUsername == null && alert.AlertType != AlertType.TransferRequest)
                {
                    alert.CaseManagerUsername = notification?.Episode?.CaseManagerUsername;
                }

                if (alert.TbServiceCode == null)
                {
                    alert.TbServiceCode = notification?.Episode?.TBServiceCode;
                }
            }

            await _alertRepository.AddAlertAsync(alert);
        }

        public async Task DismissMatchingAlertAsync(int notificationId, AlertType alertType)
        {
            var matchingAlert = await _alertRepository.GetAlertByNotificationIdAndTypeAsync(notificationId, alertType);
            if (matchingAlert != null)
            {
                await DismissAlertAsync(matchingAlert.AlertId, "System");
            }
        }

        public async Task<IList<Alert>> GetAlertsForNotificationAsync(int notificationId, ClaimsPrincipal user)
        {
            var alerts = await _alertRepository.GetAlertsForNotificationAsync(notificationId);
            var filteredAlerts = await _authorizationService.FilterAlertsForUserAsync(user, alerts);

            return filteredAlerts;
        }

        public async Task CreateAlertsForUnmatchedLabResults(IEnumerable<SpecimenMatchPairing> specimenMatchPairings)
        {
            var alerts = new List<UnmatchedLabResultAlert>();
            foreach (var specimenMatchPairing in specimenMatchPairings)
            {
                var notification = await _notificationRepository.GetNotificationForAlertCreation(
                    specimenMatchPairing.NotificationId);

                if (notification != null)
                {
                    alerts.Add(new UnmatchedLabResultAlert
                    {
                        AlertStatus = AlertStatus.Open,
                        NotificationId = notification.NotificationId,
                        SpecimenId = specimenMatchPairing.ReferenceLaboratoryNumber,
                        CaseManagerUsername = notification.Episode.CaseManagerUsername,
                        TbServiceCode = notification.Episode.TBServiceCode,
                        CreationDate = DateTime.Now
                    });
                }
            }

            await _alertRepository.AddAlertRangeAsync(alerts);
        }
    }
}
