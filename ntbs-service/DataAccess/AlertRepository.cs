﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFAuditer;
using Microsoft.EntityFrameworkCore;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;

namespace ntbs_service.DataAccess
{
    public interface IAlertRepository
    {
        Task<Alert> GetOpenAlertByIdAsync(int? alertId);
        Task<Alert> GetAlertByNotificationIdAndTypeAsync(int notificationId, AlertType alertType);
        Task<Alert> GetOpenAlertByNotificationIdAndTypeAsync(int notificationId, AlertType alertType);
        Task<IList<Alert>> GetOpenAlertsForNotificationAsync(int notificationId);
        Task<IList<Alert>> GetOpenAlertsByTbServiceCodesAsync(IEnumerable<string> tbServices);
        Task<IList<UnmatchedLabResultAlert>> GetAllOpenUnmatchedLabResultAlertsAsync();
        Task<DataQualityDraftAlert> GetOpenDraftAlertForNotificationAsync(int notificationId);
        Task AddAlertAsync(Alert alert);
        Task AddAlertRangeAsync(IEnumerable<Alert> alerts);

        Task CloseAlertRangeAsync(IEnumerable<Alert> alerts);
        Task CloseUnmatchedLabResultAlertsForSpecimenIdAsync(string specimenId);
        Task SaveAlertChangesAsync(NotificationAuditType auditType = NotificationAuditType.Edited);
    }

    public class AlertRepository : IAlertRepository
    {
        private readonly NtbsContext _context;

        public AlertRepository(NtbsContext context)
        {
            _context = context;
        }

        public async Task<Alert> GetOpenAlertByIdAsync(int? alertId)
        {
            return await GetBaseOpenAlertIQueryable()
                .SingleOrDefaultAsync(m => m.AlertId == alertId);
        }

        public async Task<Alert> GetAlertByNotificationIdAndTypeAsync(int notificationId, AlertType alertType)
        {
            return await _context.Alert
                .SingleOrDefaultAsync(m => m.NotificationId == notificationId && m.AlertType == alertType);
        }

        public async Task<Alert> GetOpenAlertByNotificationIdAndTypeAsync(int notificationId, AlertType alertType)
        {
            return await GetBaseOpenAlertIQueryable()
                .Where(a => a.NotificationId == notificationId)
                .Where(a => a.AlertType == alertType)
                .SingleOrDefaultAsync();
        }

        public async Task<IList<Alert>> GetOpenAlertsByTbServiceCodesAsync(IEnumerable<string> tbServices)
        {
            return await GetBaseOpenAlertIQueryable()
                .Where(a => tbServices.Contains(a.TbServiceCode))
                .Where(a => a.NotificationId == null || a.Notification.NotificationStatus != NotificationStatus.Draft || a.AlertType == AlertType.DataQualityDraft)
                .OrderByDescending(a => a.CreationDate)
                .ToListAsync();
        }

        public async Task<IList<UnmatchedLabResultAlert>> GetAllOpenUnmatchedLabResultAlertsAsync()
        {
            return await GetBaseOpenAlertIQueryable()
                .OfType<UnmatchedLabResultAlert>()
                .ToListAsync();
        }

        public async Task<DataQualityDraftAlert> GetOpenDraftAlertForNotificationAsync(int notificationId)
        {
            return await _context.Alert
                .Where(n => n.AlertStatus != AlertStatus.Closed)
                .Where(n => n.NotificationId == notificationId)
                .OfType<DataQualityDraftAlert>()
                .SingleOrDefaultAsync();
        }
        
        public async Task<IList<Alert>> GetOpenAlertsForNotificationAsync(int notificationId)
        {
            return await GetBaseOpenAlertIQueryable()
                .Where(a => a.NotificationId == notificationId)
                .ToListAsync();
        }

        private IQueryable<Alert> GetBaseOpenAlertIQueryable()
        {
            return _context.Alert
                .Where(n => n.AlertStatus != AlertStatus.Closed)
                .Include(n => n.TbService)
                    .ThenInclude(s => s.PHEC)
                .Include(n => n.CaseManager);
        }

        public async Task AddAlertAsync(Alert alert)
        {
            _context.Alert.Add(alert);
            await SaveAlertChangesAsync();
        }

        public async Task AddAlertRangeAsync(IEnumerable<Alert> alerts)
        {
            _context.Alert.AddRange(alerts);
            await SaveAlertChangesAsync();
        }

        public async Task CloseAlertRangeAsync(IEnumerable<Alert> alerts)
        {
            foreach (var alert in alerts)
            {
                alert.AlertStatus = AlertStatus.Closed;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CloseUnmatchedLabResultAlertsForSpecimenIdAsync(string specimenId)
        {
            var alertsToClose = await _context.Alert.OfType<UnmatchedLabResultAlert>()
                .Where(alert => alert.AlertStatus == AlertStatus.Open && alert.SpecimenId == specimenId)
                .ToListAsync();

            await CloseAlertRangeAsync(alertsToClose);
        }

        public async Task SaveAlertChangesAsync(NotificationAuditType auditType = NotificationAuditType.Edited)
        {
            _context.AddAuditCustomField(CustomFields.AuditDetails, auditType);
            await _context.SaveChangesAsync();
        }
    }
}
