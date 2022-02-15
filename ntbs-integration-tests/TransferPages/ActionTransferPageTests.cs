﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ntbs_integration_tests.Helpers;
using ntbs_service;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;
using Xunit;

namespace ntbs_integration_tests.TransferPage
{
    public class ActionTransferPageTests : TestRunnerNotificationBase
    {
        protected override string NotificationSubPath => NotificationSubPaths.ActionTransferRequest;
        public ActionTransferPageTests(NtbsWebApplicationFactory<Startup> factory) : base(factory) { }

        public static IList<Notification> GetSeedingNotifications()
        {
            return new List<Notification>()
            {
                new Notification
                {
                    NotificationId = Utilities.NOTIFIED_ID_WITH_TRANSFER_REQUEST_TO_REJECT,
                    NotificationStatus = NotificationStatus.Notified,
                    // Requires a notification site to pass full validation
                    NotificationSites = new List<NotificationSite>
                    {
                        new NotificationSite { NotificationId = Utilities.NOTIFIED_ID_WITH_TRANSFER_REQUEST_TO_REJECT, SiteId = (int)SiteId.PULMONARY }
                    },
                    HospitalDetails = new HospitalDetails
                    {
                        TBServiceCode = Utilities.TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                        HospitalId = Guid.Parse(Utilities.HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID),
                        CaseManagerId = Utilities.CASEMANAGER_ABINGDON_ID
                    }
                },
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_WITH_TRANSFER_REQUEST_TO_ACCEPT,
                    NotificationStatus = NotificationStatus.Notified,
                    ClinicalDetails = new ClinicalDetails{ DiagnosisDate = new DateTime(2020,12,12) },
                    HospitalDetails = new HospitalDetails
                    {
                        TBServiceCode = Utilities.TBSERVICE_ROYAL_FREE_LONDON_TB_SERVICE_ID,
                        CaseManagerId = Utilities.CASEMANAGER_ABINGDON_ID,
                        Consultant = "Dr John Dorian"
                    },
                    PatientDetails = new PatientDetails { LocalPatientId = "Patient Number 1234" }
                }
            };
        }

        [Fact]
        public async Task DeclineTransferAlert_ReturnsPageWithModelErrors_IfReasonNotValid()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["TransferRequest.AcceptTransfer"] = "false",
                ["TransferRequest.DeclineTransferReason"] = "£££",
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            resultDocument.AssertErrorMessage("comment", "Invalid character found in Explanatory comment");
        }

        [Fact]
        public async Task ActionTransferAlertPage_ReturnsPageWithModelErrors_IfNoChoiceMade()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, null, url);

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            resultDocument.AssertErrorMessage("action", "Please accept or decline the transfer");
        }

        [Fact]
        public async Task AcceptTransferAlert_SuccessfullyChangesTbServiceCaseManagerAndHospitalOfNotificationAndDismissesAlert()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID_WITH_NOTIFICATION_DATE;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            Assert.Equal("  ", initialDocument.QuerySelector("#banner-tb-service").TextContent);

            var formData = new Dictionary<string, string>
            {
                ["TransferRequest.AcceptTransfer"] = "true",
                ["FormattedTransferDate.Day"] = "3",
                ["FormattedTransferDate.Month"] = "12",
                ["FormattedTransferDate.Year"] = "2021",
                ["TransferRequest.TargetCaseManagerId"] = Utilities.CASEMANAGER_ABINGDON_ID.ToString(),
                ["TransferRequest.TargetHospitalId"] = Utilities.HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);
            var resultDocument = await GetDocumentAsync(result);

            // Assert
            Assert.NotNull(resultDocument.QuerySelector("#return-to-notification"));
            var overviewUrl = RouteHelper.GetNotificationPath(id, NotificationSubPaths.Overview);
            var overviewPage = await GetDocumentForUrlAsync(overviewUrl);
            Assert.Contains("Abingdon Community Hospital", overviewPage.QuerySelector("#banner-tb-service").TextContent);
            Assert.Contains("Abingdon Permitted", overviewPage.QuerySelector("#banner-case-manager").TextContent);
            Assert.Contains("ABINGDON COMMUNITY HOSPITAL", overviewPage.QuerySelector("#overview-hospital-details-hospital-name").TextContent);
            Assert.Empty(
                overviewPage.QuerySelector("#overview-patient-details-local-patient-id").TextContent.Replace(" ", ""));
            Assert.Empty(overviewPage.QuerySelector("#overview-hospital-details-consultant").TextContent.Replace(" ", ""));

            Assert.Null(overviewPage.QuerySelector("#alert-20003"));
        }

        [Fact]
        public async Task AcceptTransferAlert_CreatesTransferInAndOutTreatmentEventsForNotification()
        {
            // Arrange
            const int id = Utilities.NOTIFICATION_WITH_TRANSFER_REQUEST_TO_ACCEPT;
            var treatmentEventsUrl = RouteHelper.GetNotificationPath(id, NotificationSubPaths.EditTreatmentEvents);
            var initialTreatmentEventsPage = await GetDocumentForUrlAsync(treatmentEventsUrl);
            Assert.Null(initialTreatmentEventsPage.QuerySelector("#treatment-events"));

            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["TransferRequest.AcceptTransfer"] = "true",
                ["FormattedTransferDate.Day"] = "3",
                ["FormattedTransferDate.Month"] = "12",
                ["FormattedTransferDate.Year"] = "2021",
                ["TransferRequest.TargetCaseManagerId"] = Utilities.CASEMANAGER_ABINGDON_ID.ToString(),
                ["TransferRequest.TargetHospitalId"] = Utilities.HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID
            };

            // Act
            await Client.SendPostFormWithData(initialDocument, formData, url);

            var reloadedTreatmentEventsPage = await GetDocumentForUrlAsync(treatmentEventsUrl);

            // Assert
            var reloadedTreatmentEventsTable = reloadedTreatmentEventsPage.QuerySelector("#treatment-events");
            Assert.Contains("Transfer in", reloadedTreatmentEventsTable.InnerHtml);
            Assert.Contains("03 Dec 2021", reloadedTreatmentEventsTable.TextContent);
            Assert.Contains("Transfer out", reloadedTreatmentEventsTable.InnerHtml);
        }

        [Fact]
        public async Task AcceptTransferAlert_CreatesTransferWithDateFromFormInsteadOfDateFromAlert()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_WITH_ACTIVE_HOSPITAL;
            var treatmentEventsUrl = RouteHelper.GetNotificationPath(id, NotificationSubPaths.EditTreatmentEvents);
            var initialTreatmentEventsPage = await GetDocumentForUrlAsync(treatmentEventsUrl);
            Assert.Null(initialTreatmentEventsPage.QuerySelector("#treatment-events"));

            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["TransferRequest.AcceptTransfer"] = "true",
                ["FormattedTransferDate.Day"] = "3",
                ["FormattedTransferDate.Month"] = "12",
                ["FormattedTransferDate.Year"] = "2021",
                ["TransferRequest.TargetCaseManagerId"] = Utilities.CASEMANAGER_ABINGDON_ID.ToString(),
                ["TransferRequest.TargetHospitalId"] = Utilities.HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID
            };

            // Act
            await Client.SendPostFormWithData(initialDocument, formData, url);

            var reloadedTreatmentEventsPage = await GetDocumentForUrlAsync(treatmentEventsUrl);

            // Assert
            var reloadedTreatmentEventsTable = reloadedTreatmentEventsPage.QuerySelector("#treatment-events");
            Assert.Contains("Transfer in", reloadedTreatmentEventsTable.InnerHtml);
            Assert.Contains("03 Dec 2021", reloadedTreatmentEventsTable.TextContent);
            Assert.Contains("Transfer out", reloadedTreatmentEventsTable.InnerHtml);
        }

        [Fact]
        public async Task AcceptTransferAlert_ReturnsPageWithModelErrors_IfDateInvalid()
        {
            // Arrange
            const int id = Utilities.NOTIFICATION_WITH_TRANSFER_REQUEST_TO_ACCEPT;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["TransferRequest.AcceptTransfer"] = "true",
                ["FormattedTransferDate.Day"] = "1",
                ["FormattedTransferDate.Month"] = "11",
                ["FormattedTransferDate.Year"] = "101",
                ["TransferRequest.TargetCaseManagerId"] = Utilities.CASEMANAGER_ABINGDON_ID.ToString(),
                ["TransferRequest.TargetHospitalId"] = Utilities.HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            resultDocument.AssertErrorMessage("transfer-date", ValidationMessages.DateValidityRangeStart("Transfer date", ValidDates.EarliestAllowedDate));
        }

        [Fact]
        public async Task DeclineTransferAlert_CreatesNewTransferRejectionAlert()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID_WITH_TRANSFER_REQUEST_TO_REJECT;

            var overviewUrl = RouteHelper.GetNotificationPath(id, NotificationSubPaths.Overview);
            var overviewPage = await GetDocumentForUrlAsync(overviewUrl);
            Assert.NotNull(overviewPage.QuerySelector("#alert-20004"));

            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["TransferRequest.AcceptTransfer"] = "false",
                ["TransferRequest.DeclineTransferReason"] = "nah"
            };

            // Act
            await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var reloadedOverviewUrl = RouteHelper.GetNotificationPath(id, NotificationSubPaths.Overview);
            var reloadedOverviewPage = await GetDocumentForUrlAsync(reloadedOverviewUrl);
            var alertsContainer = reloadedOverviewPage.QuerySelector(".overview-alerts-container");
            Assert.Null(alertsContainer.QuerySelector("#alert-20004"));
            Assert.Contains("Transfer request rejected", alertsContainer.InnerHtml);
        }
        
        [Fact]
        public async Task ActionTransferPage_ReturnsCorrectStatusCode_IfNoTransferPending()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID_2;
            var actionTransferPath = GetPathForId(NotificationSubPaths.ActionTransferRequest, id);
            
            // Act
            var response = await Client.GetAsync(actionTransferPath);
            
            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(GetRedirectLocation(response), GetPathForId(NotificationSubPaths.Overview, id));
        }
    }
}
