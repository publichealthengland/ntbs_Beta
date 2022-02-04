﻿using System.Collections.Generic;
using System.Linq;
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
    public class MBovisOccupationExposurePageTests : TestRunnerNotificationBase
    {
        protected override string NotificationSubPath => NotificationSubPaths.EditMBovisOccupationExposures;

        public MBovisOccupationExposurePageTests(NtbsWebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        public static IList<Notification> GetSeedingNotifications()
        {
            return new List<Notification>
            {
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_ENTITIES,
                    NotificationStatus = NotificationStatus.Notified,
                    DrugResistanceProfile = new DrugResistanceProfile { Species = "M. bovis" },
                    MBovisDetails = new MBovisDetails
                    {
                        OccupationExposureStatus = Status.Yes,
                        MBovisOccupationExposures = new List<MBovisOccupationExposure>
                        {
                            new MBovisOccupationExposure
                            {
                                YearOfExposure = 2000,
                                OccupationDuration = 12,
                                OccupationSetting = OccupationSetting.Farm,
                                CountryId = 1
                            }
                        }
                    }
                },
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_ID_WITH_MBOVIS_NULL_OCCUPATION_NO_ENTITIES,
                    NotificationStatus = NotificationStatus.Notified,
                    DrugResistanceProfile = new DrugResistanceProfile { Species = "M. bovis" },
                    MBovisDetails = new MBovisDetails { OccupationExposureStatus = null }
                },
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_ID_WITH_MBOVIS_NO_OCCUPATION_NO_ENTITIES,
                    NotificationStatus = NotificationStatus.Notified,
                    DrugResistanceProfile = new DrugResistanceProfile { Species = "M. bovis" },
                    MBovisDetails = new MBovisDetails { OccupationExposureStatus = Status.No }
                },
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_ID_WITH_MBOVIS_UNKNOWN_OCCUPATION_NO_ENTITIES,
                    NotificationStatus = NotificationStatus.Notified,
                    DrugResistanceProfile = new DrugResistanceProfile { Species = "M. bovis" },
                    MBovisDetails = new MBovisDetails { OccupationExposureStatus = Status.Unknown }
                }
            };
        }

        [Fact]
        public async Task IfNotificationHasKnownCases_DisplaysTable()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_ENTITIES;

            // Act
            var response = await Client.GetAsync(GetCurrentPathForId(notificationId));

            // Assert
            var document = await GetDocumentAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(document.QuerySelector("#mbovis-occupation-exposure-table"));
        }

        [Theory]
        [InlineData(Utilities.NOTIFICATION_ID_WITH_MBOVIS_NULL_OCCUPATION_NO_ENTITIES)]
        [InlineData(Utilities.NOTIFICATION_ID_WITH_MBOVIS_NO_OCCUPATION_NO_ENTITIES)]
        [InlineData(Utilities.NOTIFICATION_ID_WITH_MBOVIS_UNKNOWN_OCCUPATION_NO_ENTITIES)]
        public async Task IfNotificationDoesNotHaveKnownCases_DoesNotDisplayTable(int notificationId)
        {
            // Act
            var response = await Client.GetAsync(GetCurrentPathForId(notificationId));

            // Assert
            var document = await GetDocumentAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(document.QuerySelector("#mbovis-occupation-exposure-table"));
        }

        [Fact]
        public async Task EditPage_RedirectsToOverviewWithCorrectAnchorFragmentAndSavesContent_IfModelValid()
        {
            // Arrange
            const int id = Utilities.NOTIFICATION_ID_WITH_MBOVIS_NULL_OCCUPATION_NO_ENTITIES;
            var url = GetCurrentPathForId(id);
            var document = await GetDocumentForUrlAsync(url);

            var formData = new Dictionary<string, string>
            {
                ["NotificationId"] = id.ToString(),
                ["MBovisDetails.OccupationExposureStatus"] = Status.No.ToString()
            };

            // Act
            var result = await Client.SendPostFormWithData(document, formData, url);

            // Assert
            var sectionAnchorId = OverviewSubPathToAnchorMap.GetOverviewAnchorId(NotificationSubPath);
            result.AssertRedirectTo($"/Notifications/{id}#{sectionAnchorId}");

            var reloadedPage = await Client.GetAsync(url);
            var reloadedDocument = await GetDocumentAsync(reloadedPage);
            reloadedDocument.AssertInputRadioValue("has-exposure-no", true);
        }

        [Fact]
        public async Task NotifiedPageHasReturnLinkToOverview()
        {
            // Arrange
            const int id = Utilities.NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_ENTITIES;
            var url = GetCurrentPathForId(id);

            // Act
            var document = await GetDocumentForUrlAsync(url);

            // Assert
            var overviewLink = RouteHelper.GetNotificationOverviewPathWithSectionAnchor(id, NotificationSubPath);
            Assert.NotNull(document.QuerySelector($"a[href='{overviewLink}']"));
        }

        [Fact]
        public async Task AddPage_WhenModelInvalid_ShowsExpectedValidationMessages()
        {
            // Arrange
            const int id = Utilities.NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_ENTITIES;
            var url = GetPathForId(NotificationSubPaths.AddMBovisOccupationExposure, id);
            var document = await GetDocumentForUrlAsync(url);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["MBovisOccupationExposure.YearOfExposure"] = "1100",
                ["MBovisOccupationExposure.CountryId"] = "",
                ["MBovisOccupationExposure.OccupationSetting"] = "",
                ["MBovisOccupationExposure.OccupationDuration"] = "1100",
                ["MBovisOccupationExposure.OtherDetails"] = "$£$£$£"
            };
            var result = await Client.SendPostFormWithData(document, formData, url);
            var resultDocument = await GetDocumentAsync(result);

            // Assert
            result.AssertValidationErrorResponse();

            resultDocument.AssertErrorSummaryMessage(
                "MBovisOccupationExposure-YearOfExposure",
                "year-of-exposure",
                "Year of exposure has an invalid year");
            resultDocument.AssertErrorSummaryMessage(
                "MBovisOccupationExposure-OccupationDuration",
                "duration",
                "The field Duration (years) must be between 1 and 99.");
            resultDocument.AssertErrorSummaryMessage(
                "MBovisOccupationExposure-OtherDetails",
                "other-details",
                "Invalid character found in Other details");
        }

        [Fact]
        public async Task AddPage_WhenModelValid_RedirectsToCollectionViewAndSavesChanges()
        {
            // Arrange
            const int id = Utilities.NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_ENTITIES;
            var url = GetPathForId(NotificationSubPaths.AddMBovisOccupationExposure, id);
            var document = await GetDocumentForUrlAsync(url);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["MBovisOccupationExposure.YearOfExposure"] = "2010",
                ["MBovisOccupationExposure.CountryId"] = "3",
                ["MBovisOccupationExposure.OccupationSetting"] = ((int)OccupationSetting.Vet).ToString(),
                ["MBovisOccupationExposure.OccupationDuration"] = "5",
                ["MBovisOccupationExposure.OtherDetails"] = "Some other testing details"
            };
            var result = await Client.SendPostFormWithData(document, formData, url);

            // Assert
            result.AssertRedirectTo(
                RouteHelper.GetNotificationPath(id, NotificationSubPaths.EditMBovisOccupationExposures));

            // Find the edit page for the newly added occupation exposure event. We don't know what ID the database
            // will give this event, so we can't generate the URL. Instead, we take it from the event's edit link
            var occupationExposuresDocument = await GetDocumentForUrlAsync(GetRedirectLocation(result));
            var occupationExposureUrl = occupationExposuresDocument.QuerySelectorAll(".notification-edit-link")
                .First()
                .Attributes
                .GetNamedItem("href")
                .Value;
            var newOccupationExposureDocument = await GetDocumentForUrlAsync(occupationExposureUrl);

            newOccupationExposureDocument.AssertInputTextValue("MBovisOccupationExposure_YearOfExposure", "2010");
            newOccupationExposureDocument.AssertInputSelectValue("MBovisOccupationExposure_CountryId", "3");
            newOccupationExposureDocument.AssertInputSelectValue("MBovisOccupationExposure_OccupationSetting",
                ((int)OccupationSetting.Vet).ToString());
            newOccupationExposureDocument.AssertInputTextValue("MBovisOccupationExposure_OccupationDuration", "5");
            newOccupationExposureDocument.AssertTextAreaValue("MBovisOccupationExposure_OtherDetails",
                "Some other testing details");
        }
    }
}
