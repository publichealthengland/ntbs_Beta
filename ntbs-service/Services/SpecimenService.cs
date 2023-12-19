﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ntbs_service.DataAccess;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;

namespace ntbs_service.Services
{
    public interface ISpecimenService
    {
        Task<IEnumerable<MatchedSpecimen>> GetMatchedSpecimenDetailsForNotificationAsync(int notificationId);

        Task<IEnumerable<UnmatchedSpecimen>> GetAllUnmatchedSpecimensAsync();

        Task<IEnumerable<UnmatchedSpecimen>> GetUnmatchedSpecimensDetailsForTbServicesAsync(
            IEnumerable<string> tbServiceCodes);

        Task<IEnumerable<UnmatchedSpecimen>> GetUnmatchedSpecimensDetailsForPhecsAsync(
            IEnumerable<string> phecCodes);

        Task<IEnumerable<SpecimenMatchPairing>> GetAllSpecimenPotentialMatchesAsync();

        Task<IEnumerable<(string LegacyId, string ReferenceLaboratoryNumber)>> GetLegacyReferenceLaboratoryMatches(
            IEnumerable<string> legacyIds);

        /// <summary>
        /// Calls stored proc which removes the match record
        /// </summary>
        /// <param name="notificationId">NTBS ID of the notification to match</param>
        /// <param name="labReferenceNumber">identifier of the specimen</param>
        /// <param name="userName">username to record in the audit trail</param>
        /// <returns>boolean that's true when the proc returned a success flag</returns>
        Task<bool> UnmatchSpecimenAsync(int notificationId, string labReferenceNumber, string userName, bool isPotential = false);

        /// <summary>
        /// Calls stored proc which records the match
        /// </summary>
        /// <param name="notificationId">NTBS ID of the notification to match</param>
        /// <param name="labReferenceNumber">identifier of the specimen</param>
        /// <param name="userName">username to record in the audit trail</param>
        /// <param name="isMigrating">whether currently migrating legacy notification</param>
        /// <returns>boolean that's true when the proc returned a success flag</returns>
        Task<bool> MatchSpecimenAsync(int notificationId, string labReferenceNumber, string userName, bool isMigrating = false);

        Task<bool> UnmatchAllSpecimensForNotification(int notificationId, string auditUsername);
    }

    public class SpecimenService : ISpecimenService
    {
        private readonly string _specimenMatchingDbConnectionString;
        private readonly IAuditService _auditService;
        private readonly IAlertRepository _alertRepository;

        public SpecimenService(
            IConfiguration configuration,
            IAuditService auditService,
            IAlertRepository alertRepository)
        {
            _specimenMatchingDbConnectionString =
                configuration.GetConnectionString(Constants.DbConnectionStringSpecimenMatching);
            _auditService = auditService;
            _alertRepository = alertRepository;
        }

        public async Task<IEnumerable<MatchedSpecimen>> GetMatchedSpecimenDetailsForNotificationAsync(
            int notificationId)
        {
            using (var connection = new SqlConnection(_specimenMatchingDbConnectionString))
            {
                connection.Open();
                return await connection.QueryAsync<MatchedSpecimen>(
                    SpecimenQueryHelper.GetMatchedSpecimensForNotificationQuery,
                    new { param = notificationId });
            }
        }

        public async Task<IEnumerable<UnmatchedSpecimen>> GetAllUnmatchedSpecimensAsync()
        {
            var query = SpecimenQueryHelper.GetAllUnmatchedSpecimensQuery;
            var unmatchedQueryResultRows = await ExecuteUnmatchedSpecimenQuery(query);
            return GroupPotentialMatchesByUnmatchedSpecimen(unmatchedQueryResultRows);
        }

        public async Task<IEnumerable<UnmatchedSpecimen>> GetUnmatchedSpecimensDetailsForTbServicesAsync(
            IEnumerable<string> tbServiceCodes)
        {
            var query = SpecimenQueryHelper.GetUnmatchedSpecimensForTbServicesQuery;
            var formattedTbServiceCodes = SpecimenQueryHelper.FormatEnumerableParams(tbServiceCodes);
            var unmatchedQueryResultRows = await ExecuteUnmatchedSpecimenQuery(query, formattedTbServiceCodes);
            return GroupPotentialMatchesByUnmatchedSpecimen(unmatchedQueryResultRows);
        }

        public async Task<IEnumerable<UnmatchedSpecimen>> GetUnmatchedSpecimensDetailsForPhecsAsync(
            IEnumerable<string> phecCodes)
        {
            var query = SpecimenQueryHelper.GetUnmatchedSpecimensForPhecsQuery;
            var formattedPhecCodes = SpecimenQueryHelper.FormatEnumerableParams(phecCodes);
            var unmatchedQueryResultRows = await ExecuteUnmatchedSpecimenQuery(query, formattedPhecCodes);
            return GroupPotentialMatchesByUnmatchedSpecimen(unmatchedQueryResultRows);
        }

        public async Task<IEnumerable<SpecimenMatchPairing>> GetAllSpecimenPotentialMatchesAsync()
        {
            var query = SpecimenQueryHelper.GetAllSpecimenPotentialMatchesQuery;
            using (var connection = new SqlConnection(_specimenMatchingDbConnectionString))
            {
                connection.Open();
                return await connection.QueryAsync<SpecimenMatchPairing>(query);
            }
        }

        public async Task<IEnumerable<(string LegacyId, string ReferenceLaboratoryNumber)>>
            GetLegacyReferenceLaboratoryMatches(IEnumerable<string> legacyIds)
        {
            // The table we're referencing here has legacyIds stored as INTs (since they are all ETS ids)
            // Therefore we need to convert to and from strings
            var intIds = legacyIds
                .Select(id => int.TryParse(id, out var intId) ? intId : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value);
            using (var connection = new SqlConnection(_specimenMatchingDbConnectionString))
            {
                connection.Open();
                var queryResult = await connection.QueryAsync<(int LegacyId, string ReferenceLaboratoryNumber)>(
                    SpecimenQueryHelper.LegacyReferenceLaboratoryMatchesQuery,
                    new { Ids = intIds });
                return queryResult.Select(tuple =>
                {
                    var legacyId = tuple.LegacyId.ToString();
                    return (legacyId, tuple.ReferenceLaboratoryNumber);
                });
            }
        }

        private async Task<IEnumerable<UnmatchedQueryResultRow>> ExecuteUnmatchedSpecimenQuery(
            string query,
            string param = null)
        {
            using (var connection = new SqlConnection(_specimenMatchingDbConnectionString))
            {
                connection.Open();
                return await connection.QueryAsync<SpecimenBase, SpecimenPotentialMatch, UnmatchedQueryResultRow>(
                    query,
                    (specimen, potentialMatch) => new UnmatchedQueryResultRow
                    {
                        SpecimenBase = specimen,
                        SpecimenPotentialMatch = potentialMatch
                    },
                    splitOn: nameof(SpecimenPotentialMatch.NotificationId),
                    param: new { param });
            }
        }

        private static IEnumerable<UnmatchedSpecimen> GroupPotentialMatchesByUnmatchedSpecimen(
            IEnumerable<UnmatchedQueryResultRow> unmatchedResultRows)
        {
            return unmatchedResultRows.GroupBy(
                row => row.SpecimenBase.ReferenceLaboratoryNumber,
                row => row,
                (referenceNumber, rows) =>
                {
                    var groupedRows = rows.ToList();
                    var specimenData = groupedRows.First().SpecimenBase;
                    return new UnmatchedSpecimen
                    {
                        ReferenceLaboratoryNumber = specimenData.ReferenceLaboratoryNumber,
                        SpecimenDate = specimenData.SpecimenDate,
                        SpecimenTypeCode = specimenData.SpecimenTypeCode,
                        LaboratoryName = specimenData.LaboratoryName,
                        Species = specimenData.Species,
                        LabNhsNumber = specimenData.LabNhsNumber,
                        LabBirthDate = specimenData.LabBirthDate,
                        LabName = specimenData.LabName,
                        LabSex = specimenData.LabSex,
                        LabAddress = specimenData.LabAddress,
                        LabPostcode = specimenData.LabPostcode,

                        PotentialMatches = groupedRows.Select(r => r.SpecimenPotentialMatch)
                    };
                });
        }

        public async Task<bool> UnmatchSpecimenAsync(int notificationId, string labReferenceNumber, string userName, bool isPotential = false)
        {
            using (var connection = new SqlConnection(_specimenMatchingDbConnectionString))
            {
                connection.Open();
                var returnValue = await connection.QuerySingleAsync<int>(
                    @"DECLARE @result int;  
                        exec @result = uspUnmatchSpecimen @referenceLaboratoryNumber, @notificationId;
                        SELECT @result",
                    new { referenceLaboratoryNumber = labReferenceNumber, notificationId });

                var success = returnValue == 0;

                if (success)
                {
                    if (isPotential)
                    {
                        await _auditService.AuditRejectPotentialSpecimen(notificationId,
                            labReferenceNumber,
                            userName,
                            NotificationAuditType.Edited);
                    }
                    else
                    {
                        await _auditService.AuditUnmatchSpecimen(notificationId,
                            labReferenceNumber,
                            userName,
                            NotificationAuditType.Edited);
                    }
                    await _alertRepository.CloseUnmatchedLabResultAlertForSpecimenAndNotificationAsync(labReferenceNumber, notificationId);
                }

                return success;
            }
        }

        public async Task<bool> MatchSpecimenAsync(int notificationId, string labReferenceNumber, string userName, bool isMigrating = false)
        {
            using (var connection = new SqlConnection(_specimenMatchingDbConnectionString))
            {
                connection.Open();
                var returnValue = await connection.QuerySingleAsync<int>(
                    @"DECLARE @result int;  
                    exec @result = uspMatchSpecimen @referenceLaboratoryNumber, @notificationId, @isMigrating;
                    SELECT @result",
                    new { referenceLaboratoryNumber = labReferenceNumber, notificationId, isMigrating });

                var success = returnValue == 0;

                if (success)
                {
                    var auditType = isMigrating ? NotificationAuditType.Imported : NotificationAuditType.Edited;
                    await _auditService.AuditMatchSpecimen(notificationId, labReferenceNumber, userName, auditType);
                    await _alertRepository.CloseUnmatchedLabResultAlertsForSpecimenIdAsync(labReferenceNumber);
                }

                return success;
            }
        }

        public async Task<bool> UnmatchAllSpecimensForNotification(int notificationId, string auditUsername)
        {
            var existingMatches = await GetMatchedSpecimenDetailsForNotificationAsync(notificationId);
            var labNumbers = existingMatches.Select(match => match.ReferenceLaboratoryNumber);

            var overallResult = true;
            foreach (var labNumber in labNumbers)
            {
                var resultForLabNumber = await UnmatchSpecimenAsync(notificationId, labNumber, auditUsername);
                overallResult &= resultForLabNumber;
            }
            return overallResult; // Only true if all were successful
        }

        private class UnmatchedQueryResultRow
        {
            internal SpecimenBase SpecimenBase { get; set; }

            internal SpecimenPotentialMatch SpecimenPotentialMatch { get; set; }
        }
    }
}
