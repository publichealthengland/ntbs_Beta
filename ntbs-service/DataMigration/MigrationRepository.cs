using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using ntbs_service.DataMigration.Exceptions;
using ntbs_service.Models.Entities;

namespace ntbs_service.DataMigration
{
    public interface IMigrationRepository
    {
        Task MarkNotificationsAsImportedAsync(ICollection<Notification> notifications);
        Task<IEnumerable<IGrouping<string, string>>> GetGroupedNotificationIdsById(IEnumerable<string> legacyIds);
        Task<IEnumerable<IGrouping<string, string>>> GetGroupedNotificationIdsByDate(DateTime rangeStartDate,
            DateTime endStartDate);
        Task<IEnumerable<dynamic>> GetNotificationsById(List<string> legacyIds);
        Task<IEnumerable<dynamic>> GetNotificationSites(IEnumerable<string> legacyIds);
        Task<IEnumerable<dynamic>> GetManualTestResults(List<string> legacyIds);
        Task<IEnumerable<dynamic>> GetSocialContextVenues(List<string> legacyIds);
        Task<IEnumerable<dynamic>> GetSocialContextAddresses(List<string> legacyIds);
        Task<IEnumerable<dynamic>> GetTransferEvents(List<string> legacyIds);
    }

    public class MigrationRepository : IMigrationRepository
    {
        const string NotificationIdsWithGroupIdsQuery = @"
            SELECT n.OldNotificationId, n.GroupId
            FROM MigrationNotificationsView n
            WHERE GroupId IN (
                SELECT GroupId
                FROM MigrationNotificationsView n 
                {0}
            )";
        readonly string NotificationIdsWithGroupIdsByIdQuery = string.Format(NotificationIdsWithGroupIdsQuery, "WHERE n.OldNotificationId IN @Ids OR n.GroupId IN @Ids");
        readonly string NotificationsIdsWithGroupIdsByDateQuery = string.Format(NotificationIdsWithGroupIdsQuery, @"WHERE n.NotificationDate >= @StartDate AND n.NotificationDate < @EndDate");
        
        const string NotificationsByIdQuery = @"
            SELECT *
            FROM MigrationNotificationsView n
            WHERE n.OldNotificationId IN @Ids";

        const string NotificationSitesQuery = @"
            SELECT *
            FROM NotificationSite
            WHERE OldNotificationId IN @Ids
        ";
        
        const string ManualTestResultsQuery = @"
            SELECT *
            FROM ManualTestResults
            WHERE OldNotificationId IN @Ids
        ";
        
        const string SocialContextVenuesQuery = @"
            SELECT *
            FROM MigrationSocialContextVenueView
            WHERE OldNotificationId IN @Ids
        ";
        
        const string SocialContextAddressesQuery = @"
            SELECT *
            FROM MigrationSocialContextAddressView
            WHERE OldNotificationId IN @Ids
        ";
        
        const string TransferEventsQuery = @"
            SELECT *
            FROM MigrationTransferEventsView
            WHERE OldNotificationId IN @Ids
        ";
        
        private readonly string connectionString;
        private readonly INotificationImportHelper _importHelper;

        public MigrationRepository(IConfiguration _configuration, INotificationImportHelper importHelper)
        {
            connectionString = _configuration.GetConnectionString("migration");
            _importHelper = importHelper;
        }

        public async Task MarkNotificationsAsImportedAsync(ICollection<Notification> notifications)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    var importedAt = DateTime.Now.ToString("s");

                    foreach (var notification in notifications)
                    {
                        await connection.ExecuteAsync(
                            _importHelper.InsertImportedNotificationQuery, 
                            new {notification.LegacyId, ImportedAt = importedAt}
                        );
                    }
                }
                catch (Exception exception)
                {
                    throw new MarkingNotificationsAsImportedFailedException(notifications, exception);
                }
            }
        }

        public async Task<IEnumerable<IGrouping<string, string>>> GetGroupedNotificationIdsById(
            IEnumerable<string> legacyIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var tuples = await connection.QueryAsync<string, string, Tuple<string, string>>(
                    NotificationIdsWithGroupIdsByIdQuery,
                    Tuple.Create,
                    new { Ids = legacyIds });
                return tuples.GroupBy(t => t.Item2, t => t.Item1);
            }
        }

        public async Task<IEnumerable<IGrouping<string, string>>> GetGroupedNotificationIdsByDate(
            DateTime rangeStartDate,
            DateTime endStartDate)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var tuples = await connection.QueryAsync<string, string, Tuple<string, string>>(
                    NotificationsIdsWithGroupIdsByDateQuery,
                    Tuple.Create,
                    new {StartDate = rangeStartDate.ToString("s"), EndDate = endStartDate.ToString("s")});
                return tuples.GroupBy(t => t.Item2, t => t.Item1);
            }
        }

        public async Task<IEnumerable<dynamic>> GetNotificationsById(List<string> legacyIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return await connection.QueryAsync(NotificationsByIdQuery, new {Ids = legacyIds});
            }
        }

        public async Task<IEnumerable<dynamic>> GetNotificationSites(IEnumerable<string> legacyIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return await connection.QueryAsync(NotificationSitesQuery, new {Ids = legacyIds});
            }
        }

        public async Task<IEnumerable<dynamic>> GetManualTestResults(List<string> legacyIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return await connection.QueryAsync(ManualTestResultsQuery, new {Ids = legacyIds});
            }
        }

        public async Task<IEnumerable<dynamic>> GetSocialContextVenues(List<string> legacyIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return await connection.QueryAsync(SocialContextVenuesQuery, new {Ids = legacyIds});
            }
        }

        public async Task<IEnumerable<dynamic>> GetSocialContextAddresses(List<string> legacyIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return await connection.QueryAsync(SocialContextAddressesQuery, new {Ids = legacyIds});
            }
        }

        public async Task<IEnumerable<dynamic>> GetTransferEvents(List<string> legacyIds)
        {
            
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return await connection.QueryAsync(TransferEventsQuery, new {Ids = legacyIds});
            }
        }
    }
}
