﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ntbs_integration_tests.Helpers;
using ntbs_integration_tests.TestServices;
using ntbs_service;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;
using Xunit;

namespace ntbs_integration_tests.NotificationPages
{
    public class DraftEditPageTests : TestRunnerNotificationBase
    {
        public DraftEditPageTests(NtbsWebApplicationFactory<EntryPoint> factory) : base(factory) { }

        public static IEnumerable<Notification> GetSeedingNotifications()
        {
            return new List<Notification>()
            {
                new Notification
                {
                    NotificationId = Utilities.DRAFT_NOTIFICATION_WITH_DRAFT_ALERT,
                    NotificationStatus = NotificationStatus.Draft
                }
            };
        }

        public static IEnumerable<Alert> GetSeedingAlerts()
        {
            return new List<Alert>
            {
                new DataQualityDraftAlert
                {
                    AlertId = Utilities.DRAFT_DATA_QUALITY_ALERT,
                    NotificationId = Utilities.DRAFT_NOTIFICATION_WITH_DRAFT_ALERT
                }
            };
        }

        [Fact]
        public async Task Get_ReturnsEditPageWithAlert_IfDraftHasDraftAlert()
        {
            // Arrange
            const int id = Utilities.DRAFT_NOTIFICATION_WITH_DRAFT_ALERT;
            var patientEditPageUrl = RouteHelper.GetNotificationPath(id, NotificationSubPaths.EditPatientDetails);

            // Act
            var patientEditPage = await GetDocumentForUrlAsync(patientEditPageUrl);

            // Assert
            Assert.NotNull(patientEditPage.GetElementById("draft-alert-details"));
        }

        [Fact]
        public async Task Get_ReturnsRedirectToOverview_ForReadOnlyUser()
        {
            // Arrange
            const int id = Utilities.DRAFT_NOTIFICATION_WITH_DRAFT_ALERT;
            using (var client = Factory.WithUserAuth(TestUser.ReadOnlyUser)
                .CreateClientWithoutRedirects())
            {
                // Act
                var response = await client.GetAsync(RouteHelper.GetNotificationPath(id, NotificationSubPaths.EditPatientDetails));

                // Assert
                response.AssertRedirectTo($"/Notifications/{id}");
            }
        }
    }
}
