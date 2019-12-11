using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ntbs_service.Models.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using ntbs_service.Models.Enums;

namespace ntbs_service.DataMigration
{
    public interface INotificationMapper
    {
        Task<List<List<Notification>>> GetById(List<string> notificationId);
        Task<List<List<Notification>>> GetByDate(DateTime cutoffDate);
    }

    public class NotificationMapper : INotificationMapper
    {
        const string NotificationsQuery = @"
            SELECT *,
            	trvl.Country1 AS travel_Country1,
                trvl.Country2 AS travel_Country2,
                trvl.Country3 AS travel_Country3,
                trvl.TotalNumberOfCountries AS travel_TotalNumberOfCountries,
                trvl.StayLengthInMonths1 AS travel_StayLengthInMonths1,
                trvl.StayLengthInMonths2 AS travel_StayLengthInMonths2,
                trvl.StayLengthInMonths3 AS travel_StayLengthInMonths3,
                vstr.Country1 AS visitor_Country1,
                vstr.Country2 AS visitor_Country2,
                vstr.Country3 AS visitor_Country3,
                vstr.TotalNumberOfCountries AS visitor_TotalNumberOfCountries,
                vstr.StayLengthInMonths1 AS visitor_StayLengthInMonths1,
                vstr.StayLengthInMonths2 AS visitor_StayLengthInMonths2,
                vstr.StayLengthInMonths3 AS visitor_StayLengthInMonths3
            FROM Notifications n 
            LEFT JOIN Addresses addrs ON addrs.OldNotificationId = n.OldNotificationId
            LEFT JOIN Demographics dmg ON dmg.OldNotificationId = n.OldNotificationId
            LEFT JOIN DeathDates dd ON dd.OldNotificationId = n.OldNotificationId
            LEFT JOIN VisitorHistory vstr ON vstr.OldNotificationId = n.OldNotificationId
            LEFT JOIN TravelHistory trvl ON trvl.OldNotificationId = n.OldNotificationId
            LEFT JOIN ClinicalDates clncl ON clncl.OldNotificationId = n.OldNotificationId
            LEFT JOIN Comorbidities cmrbd ON cmrbd.OldNotificationId = n.OldNotificationId
            LEFT JOIN ImmunoSuppression immn ON immn.OldNotificationId = n.OldNotificationId
            WHERE GroupId IN (
                SELECT GroupId
                FROM Notifications n {0}
            )";

        const string WhereClauseById = @"WHERE n.OldNotificationId IN ({0})";
        const string WhereClauseByDate = @"WHERE n.NotificationDate > {0}";

        const string NotificationSitesQuery = @"
            SELECT *
            FROM DiseaseSites
            WHERE OldNotificationId IN ({0})
        ";

        private readonly string connectionString;

        public NotificationMapper(IConfiguration _configuration)
        {
            connectionString = _configuration.GetConnectionString("migration");
        }

        private static NotificationSite AsNotificationSite(dynamic result)
        {
            return new NotificationSite {
                SiteId = result.DiseaseSiteId,
                SiteDescription = result.DiseaseSiteText
            };
        }

        public async Task<List<List<Notification>>> GetByDate(DateTime cutoffDate)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var whereClause = string.Format(WhereClauseByDate, $@"'{cutoffDate.ToString("s")}'");
                var queryWithWhereClause = string.Format(NotificationsQuery, whereClause);

                var notificationsRaw = await connection.QueryAsync(queryWithWhereClause);

                var legacyIds = notificationsRaw.Select(x => x.OldNotificationId).Cast<string>();
                
                var idsForInClause = string.Join(",", legacyIds.Select(x => $@"'{x}'"));
                var notificationSitesRaw = await connection.QueryAsync(string.Format(NotificationSitesQuery, idsForInClause));

                return GetGroupedResultsAsNotification(notificationsRaw, notificationSitesRaw);
            }
        }

        public async Task<List<List<Notification>>> SearchById(List<string> notificationIds)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var whereClause = string.Format(WhereClauseById, string.Join(",", notificationIds.Select(n => $@"'{n}'")));
                var queryWithWhereClause = string.Format(NotificationsQuery, whereClause);

                var notificationsRaw = await connection.QueryAsync(queryWithWhereClause);

                var legacyIds = notificationsRaw.Select(x => x.OldNotificationId).Cast<string>();
                
                var idsForInClause = string.Join(",", legacyIds.Select(x => $@"'{x}'"));
                var notificationSitesRaw = await connection.QueryAsync(string.Format(NotificationSitesQuery, idsForInClause));

                return GetGroupedResultsAsNotification(notificationsRaw, notificationSitesRaw);
            }
        }

        private List<List<Notification>> GetGroupedResultsAsNotification(IEnumerable<dynamic> notificationsRaw, IEnumerable<dynamic> notificationSitesRaw)
        {
            var groupedResults = new List<List<Notification>>();
            var notificationGroups = notificationsRaw.GroupBy(x => x.GroupId);
            var notificationSiteGroups = notificationSitesRaw.GroupBy(x => x.OldNotificationId);
            
            foreach (var notificationGroup in notificationGroups)
            {
                var notifications = notificationGroup.Select(AsNotification).ToList();
                foreach (var notificationSiteGroup in notificationSiteGroups)
                {
                    notifications.FirstOrDefault(x => x.LegacyId == notificationSiteGroup.Key).NotificationSites = notificationSiteGroup.Select(AsNotificationSite).ToList();
                }
                groupedResults.Add(notifications);
            }

            return groupedResults;
        }

        private static Notification AsNotification(dynamic result)
        {
            var notification = new Notification
            {
                ETSID = result.ETSID?.ToString(),
                LTBRID = result.LTBRID?.ToString(),
                NotificationDate = result.NotificationDate,
                CreationDate = result.CreationDate,
                PatientDetails = ExtractPatientDetails(result),
                ClinicalDetails = ExtractClinicalDetails(result),
                TravelDetails = ExtractTravelDetails(result),
                VisitorDetails = ExtractVisitorDetails(result),
                ComorbidityDetails = ExtractComorbidityDetails(result),
                ImmunosuppressionDetails = ExtractImmunosuppressionDetails(result),
                NotificationStatus = NotificationStatus.Notified
            };

            return notification;
        }

        private static ImmunosuppressionDetails ExtractImmunosuppressionDetails(dynamic notification)
        {
            return new ImmunosuppressionDetails 
            {
                Status = GetStatusFromString(notification.Status),
                HasBioTherapy = GetBoolValue(notification.HasBioTherapy),
                HasTransplantation = GetBoolValue(notification.HasTransplantation),
                HasOther = GetBoolValue(notification.HasOther),
                OtherDescription = notification.OtherDescription
            };
        }

        private static ComorbidityDetails ExtractComorbidityDetails(dynamic notification)
        {
            return new ComorbidityDetails 
            {
                DiabetesStatus = GetStatusFromString(notification.DiabetesStatus),
                LiverDiseaseStatus = GetStatusFromString(notification.LiverDiseaseStatus),
                RenalDiseaseStatus = GetStatusFromString(notification.RenalDiseaseStatus),
                HepatitisBStatus = GetStatusFromString(notification.HepatitisBStatus),
                HepatitisCStatus = GetStatusFromString(notification.HepatitisCStatus)
            };
        }

        private static ClinicalDetails ExtractClinicalDetails(dynamic notification)
        {
            return new ClinicalDetails
                {
                    SymptomStartDate = notification.SymptomStartDate,
                    FirstPresentationDate = notification.FirstPresentationDate,
                    TBServicePresentationDate = notification.TbServicePresentationDate,
                    DiagnosisDate = notification.DiagnosisDate,
                    DidNotStartTreatment = GetBoolValue(notification.DidNotStartTreatment),
                    MDRTreatmentStartDate = notification.StartOfTreatmentDay,
                    IsSymptomatic = GetBoolValue(notification.IsSymptomatic),
                    DeathDate = notification.DeathDate
                };
        }

        private static TravelDetails ExtractTravelDetails(dynamic notification)
        {
            return new TravelDetails
                {
                    HasTravel = GetBoolValue(notification.HasTravel),
                    TotalNumberOfCountries = notification.travel_TotalNumberOfCountries,
                    Country1Id = notification.travel_Country1,
                    Country2Id = notification.travel_Country2,
                    Country3Id = notification.travel_Country3,
                    StayLengthInMonths1 = notification.StayLengthInMonths1,                    
                    StayLengthInMonths2 = notification.StayLengthInMonths2,
                    StayLengthInMonths3 = notification.StayLengthInMonths3
                };
        }

        private static VisitorDetails ExtractVisitorDetails(dynamic notification)
        {
            return new VisitorDetails
                {
                    HasVisitor = GetBoolValue(notification.HasVisitor),
                    TotalNumberOfCountries = notification.visitor_TotalNumberOfCountries,
                    Country1Id = notification.visitor_Country1,
                    Country2Id = notification.visitor_Country2,
                    Country3Id = notification.visitor_Country3,
                    StayLengthInMonths1 = notification.visitor_StayLengthInMonths1,
                    StayLengthInMonths2 = notification.visitor_StayLengthInMonths2,
                    StayLengthInMonths3 = notification.visitor_StayLengthInMonths3
                };
        }

        private static PatientDetails ExtractPatientDetails(dynamic notification)
        {
            return new PatientDetails
                {
                    FamilyName = notification.FamilyName,
                    GivenName = notification.GivenName,
                    NhsNumber = notification.NhsNumber,
                    Dob = notification.DateOfBirth,
                    YearOfUkEntry = notification.UkEntryYear,
                    UkBorn = GetBoolValue(notification.UkBorn),
                    CountryId = notification.BirthCountryId,
                    LocalPatientId = notification.LocalPatientId,
                    Postcode = notification.Postcode,
                    Address = notification.Line1 + " " + notification.Line2,
                    EthnicityId = notification.NtbsEthnicGroupId,
                    SexId = notification.NtbsSexId,
                    NhsNumberNotKnown = notification.NhsNumberNotKnown == 1,
                    NoFixedAbode = notification.NoFixedAbode == 1,
                    OccupationId = notification.NtbsOccupationId,
                    OccupationOther = notification.NtbsOccupationFreeText
                };
        }

        private static bool? GetBoolValue(int? value)
        {
            if (value == null)
            {
                return null;
            }
            return value == 1 ? true : false;
        }

        private static Status? GetStatusFromString(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return null;
            }

            return Enum.Parse<Status>(status);
        }
    } 
}