﻿using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ntbs_integration_tests.Helpers;
using ntbs_integration_tests.TestServices;
using ntbs_service;
using ntbs_service.Helpers;
using Xunit;

namespace ntbs_integration_tests.NotificationPages
{
    public class ChangesPageTests : TestRunnerNotificationBase
    {
        public ChangesPageTests(NtbsWebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Get_RendersPage_ForUserWithAccessToNotification()
        {
            // Arrange
            using (var client = Factory
                .WithUserAuth(TestUser.NhsUserForAbingdonAndPermitted)
                .WithNotificationAndTbServiceConnected(Utilities.NOTIFIED_ID, Utilities.PERMITTED_SERVICE_CODE)
                .CreateClientWithoutRedirects())
            {
                // Act
                var changesPath = GetPathForId(NotificationSubPaths.NotificationChanges, Utilities.NOTIFIED_ID);
                var response = await client.GetAsync(changesPath);

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task Get_RendersCorrectNavigationLinks()
        {
            // Arrange
            using (var client = Factory
                .WithUserAuth(TestUser.NhsUserForAbingdonAndPermitted)
                .WithNotificationAndTbServiceConnected(Utilities.LINKED_NOTIFICATION_ABINGDON_TB_SERVICE, Utilities.PERMITTED_SERVICE_CODE)
                .CreateClientWithoutRedirects())
            {
                // Act
                var changesPath = GetPathForId(NotificationSubPaths.NotificationChanges, Utilities.LINKED_NOTIFICATION_ABINGDON_TB_SERVICE);
                var response = await client.GetAsync(changesPath);
                var document = await GetDocumentAsync(response);

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var navLinks = document.GetElementsByClassName("app-subnav__link");
                Assert.NotNull(navLinks.SingleOrDefault(elem => elem.TextContent.Contains("Notification details")));
                Assert.NotNull(navLinks.SingleOrDefault(elem => elem.TextContent.Contains("Linked notifications (1)")));
                Assert.NotNull(navLinks.SingleOrDefault(elem => elem.TextContent.Contains("Notification changes")));
            }
        }

        [Fact]
        public async Task Get_ReturnsRedirectToOverview_ForUserWithoutAccessToNotification()
        {
            // Arrange
            using (var client = Factory
                .WithUserAuth(TestUser.NhsUserForAbingdonAndPermitted)
                .WithNotificationAndTbServiceConnected(Utilities.NOTIFIED_ID, Utilities.UNPERMITTED_SERVICE_CODE)
                .CreateClientWithoutRedirects())
            {
                // Act
                var changesPath = GetPathForId(NotificationSubPaths.NotificationChanges, Utilities.NOTIFIED_ID);
                var response = await client.GetAsync(changesPath);

                // Assert
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                var redirectedTo = response.Headers.GetValues("Location").Single();
                Assert.Equal($"/Notifications/{Utilities.NOTIFIED_ID}", redirectedTo);
            }
        }
    }
}
