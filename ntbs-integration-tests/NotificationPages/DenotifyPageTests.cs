﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ntbs_integration_tests.Helpers;
using ntbs_service;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using Xunit;

namespace ntbs_integration_tests.NotificationPages
{
    public class DenotifyPageTests : TestRunnerNotificationBase
    {
        protected override string NotificationSubPath => NotificationSubPaths.Denotify;

        public DenotifyPageTests(NtbsWebApplicationFactory<EntryPoint> factory) : base(factory) { }

        public static IList<Notification> GetSeedingNotifications()
        {
            return new List<Notification>()
            {
                new Notification()
                {
                    NotificationId = Utilities.DENOTIFY_WITH_DESCRIPTION,
                    NotificationStatus = NotificationStatus.Notified,
                    TreatmentEvents = new List<TreatmentEvent> { new TreatmentEvent
                        {
                            TreatmentEventId = 4040,
                            TreatmentEventType = TreatmentEventType.DiagnosisMade, EventDate = new DateTime(2011, 2, 1)
                        }
                    }
                },
                new Notification()
                {
                    NotificationId = Utilities.DENOTIFY_NO_DESCRIPTION,
                    NotificationStatus = NotificationStatus.Notified,
                    TreatmentEvents = new List<TreatmentEvent> { new TreatmentEvent
                        {
                            TreatmentEventId = 4041,
                            TreatmentEventType = TreatmentEventType.DiagnosisMade, EventDate = new DateTime(2011, 2, 1)
                        }
                    }
                },
                new Notification()
                {
                    NotificationId = Utilities.NOTIFIED_ID_WITH_NOTIFICATION_DATE,
                    NotificationStatus = NotificationStatus.Notified,
                    NotificationDate = new DateTime(2011, 1, 1),
                    TreatmentEvents = new List<TreatmentEvent> { new TreatmentEvent
                        {
                            TreatmentEventId = 4042,
                            TreatmentEventType = TreatmentEventType.DiagnosisMade, EventDate = new DateTime(2011, 2, 1)
                        }
                    }
                }
            };
        }

        public static IEnumerable<object[]> DenotifyRoutes()
        {
            yield return new object[] { Utilities.DRAFT_ID, HttpStatusCode.Redirect };
            yield return new object[] { Utilities.DENOTIFIED_ID, HttpStatusCode.Redirect };
            yield return new object[] { Utilities.NOTIFIED_ID, HttpStatusCode.OK };
            yield return new object[] { Utilities.NEW_ID, HttpStatusCode.NotFound };
        }

        [Theory, MemberData(nameof(DenotifyRoutes))]
        public async Task GetDenotifyPage_ReturnsCorrectStatusCode_DependentOnId(int id, HttpStatusCode code)
        {
            // Act
            var response = await Client.GetAsync(GetCurrentPathForId(id));

            // Assert
            Assert.Equal(code, response.StatusCode);

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                // Flipped expected/actual here to accomodate trailing slash
                Assert.Contains(GetRedirectLocation(response), GetPathForId(NotificationSubPaths.Overview, id));
            }
        }

        [Fact]
        public async Task Post_RedirectsToNextPageAndDenotifies_IfModelValidWithNoDescription()
        {
            // Arrange
            const int id = Utilities.DENOTIFY_NO_DESCRIPTION;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            const string denotifyDateDay = "1";
            const string denotifyDateMonth = "1";
            const string denotifyDateYear = "2000";
            const string reason = "DuplicateEntry";

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = denotifyDateDay,
                ["FormattedDenotificationDate.Month"] = denotifyDateMonth,
                ["FormattedDenotificationDate.Year"] = denotifyDateYear,
                ["DenotificationDetails.Reason"] = reason
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            // Flipped expected/actual here to accomodate trailing slash
            Assert.Contains(GetRedirectLocation(result), GetPathForId(NotificationSubPaths.Overview, id));

            var redirectPage = await Client.GetAsync(GetRedirectLocation(result));
            var redirectDocument = await GetDocumentAsync(redirectPage);
            Assert.Contains("notification-banner--denotified", redirectDocument?.QuerySelector("dl.notification-banner")?.ClassList);
        }

        [Fact]
        public async Task Post_RedirectsToNextPageAndDenotifies_IfModelValidWithDescription()
        {
            // Arrange
            const int id = Utilities.DENOTIFY_WITH_DESCRIPTION;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = "1",
                ["FormattedDenotificationDate.Month"] = "1",
                ["FormattedDenotificationDate.Year"] = "2010",
                ["DenotificationDetails.Reason"] = "DuplicateEntry",
                ["DenotificationDetails.OtherDescription"] = "Test Description"
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            // Flipped expected/actual here to accomodate trailing slash
            Assert.Contains(GetRedirectLocation(result), GetPathForId(NotificationSubPaths.Overview, id));

            var redirectPage = await Client.GetAsync(GetRedirectLocation(result));
            var redirectDocument = await GetDocumentAsync(redirectPage);
            Assert.Contains("notification-banner--denotified", redirectDocument?.QuerySelector("dl.notification-banner")?.ClassList);
        }

        [Fact]
        public async Task Post_ReturnsPageWithModelErrors_IfDateInvalid()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = "0",
                ["FormattedDenotificationDate.Month"] = "1",
                ["FormattedDenotificationDate.Year"] = "2000",
                ["DenotificationDetails.Reason"] = "DuplicateEntry"
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            var resultDocument = await GetDocumentAsync(result);

            result.EnsureSuccessStatusCode();
            resultDocument.AssertErrorMessage("date", "Denotification date does not have a valid date selection");
        }

        [Fact]
        public async Task Post_ReturnsPageWithModelErrors_IfDateInFuture()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            const string denotifyDateDay = "1";
            const string denotifyDateMonth = "1";
            const string denotifyDateYear = "2099";
            const string reason = "DuplicateEntry";

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = denotifyDateDay,
                ["FormattedDenotificationDate.Month"] = denotifyDateMonth,
                ["FormattedDenotificationDate.Year"] = denotifyDateYear,
                ["DenotificationDetails.Reason"] = reason
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            var resultDocument = await GetDocumentAsync(result);

            result.EnsureSuccessStatusCode();
            resultDocument.AssertErrorMessage("date", "Date of denotification cannot be later than today");
        }

        [Fact]
        public async Task Post_ReturnsPageWithModelErrors_IfDateIsBeforeDateOfNotification()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID_WITH_NOTIFICATION_DATE;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            const string denotifyDateDay = "1";
            const string denotifyDateMonth = "1";
            const string denotifyDateYear = "2010";
            const string reason = "DuplicateEntry";

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = denotifyDateDay,
                ["FormattedDenotificationDate.Month"] = denotifyDateMonth,
                ["FormattedDenotificationDate.Year"] = denotifyDateYear,
                ["DenotificationDetails.Reason"] = reason
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            result.EnsureSuccessStatusCode();
            resultDocument.AssertErrorMessage("date", "Denotification date must be after the date of notification");
        }

        [Fact]
        public async Task Post_ReturnsPageWithModelErrors_IfMissingReason()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            const string denotifyDateDay = "1";
            const string denotifyDateMonth = "1";
            const string denotifyDateYear = "2000";

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = denotifyDateDay,
                ["FormattedDenotificationDate.Month"] = denotifyDateMonth,
                ["FormattedDenotificationDate.Year"] = denotifyDateYear
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            var resultDocument = await GetDocumentAsync(result);

            result.EnsureSuccessStatusCode();
            resultDocument.AssertErrorMessage("reason", "Please supply a reason for denotification");
        }

        [Fact]
        public async Task Post_ReturnsPageWithModelErrors_IfReasonOtherMissingDescription()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var url = GetCurrentPathForId(id);
            var initialDocument = await GetDocumentForUrlAsync(url);

            const string denotifyDateDay = "1";
            const string denotifyDateMonth = "1";
            const string denotifyDateYear = "2000";
            const string reason = "Other";

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["FormattedDenotificationDate.Day"] = denotifyDateDay,
                ["FormattedDenotificationDate.Month"] = denotifyDateMonth,
                ["FormattedDenotificationDate.Year"] = denotifyDateYear,
                ["DenotificationDetails.Reason"] = reason
            };

            // Act
            var result = await Client.SendPostFormWithData(initialDocument, formData, url, "Confirm");

            // Assert
            var resultDocument = await GetDocumentAsync(result);

            result.EnsureSuccessStatusCode();
            resultDocument.AssertErrorMessage("description", "Please supply additional details for the denotification reason");
        }
    }
}
