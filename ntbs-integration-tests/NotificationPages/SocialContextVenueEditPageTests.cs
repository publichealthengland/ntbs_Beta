﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ntbs_integration_tests.Helpers;
using ntbs_service;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.Validations;
using Xunit;

namespace ntbs_integration_tests.NotificationPages
{
    public class SocialContextVenueEditPageTests : TestRunnerNotificationBase
    {
        private const int VENUE_ID = 10;
        private const int VENUE_TO_DELETE_ID = 11;
        protected override string NotificationSubPath => NotificationSubPaths.EditSocialContextVenueSubPath;

        public SocialContextVenueEditPageTests(NtbsWebApplicationFactory<EntryPoint> factory) : base(factory)
        {
        }

        public static IList<Notification> GetSeedingNotifications()
        {
            return new List<Notification>
            {
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_WITH_VENUES,
                    NotificationStatus = NotificationStatus.Notified,
                    SocialContextVenues = new List<SocialContextVenue>()
                    {
                        new SocialContextVenue
                        {
                            SocialContextVenueId = VENUE_ID,
                            DateFrom = new DateTime(2012, 1, 1),
                            DateTo = new DateTime(2013, 1, 1),
                            Name = "Test venue",
                            Address = "Test address",
                            VenueTypeId = 1
                        },
                        new SocialContextVenue
                        {
                            SocialContextVenueId = VENUE_TO_DELETE_ID,
                            DateFrom = new DateTime(2012, 1, 1),
                            DateTo = new DateTime(2013, 1, 1),
                            Name = "Test venue 2",
                            Address = "Test address 2",
                            VenueTypeId = 2
                        },
                    }
                }
            };
        }

        [Fact]
        public async Task PostNewVenue_ReturnsSuccessAndAddsResultToTable()
        {
            // Arrange
            const int notificationId = Utilities.DRAFT_ID;
            var url = GetPathForId(NotificationSubPaths.AddSocialContextVenue, notificationId);
            var initialDocument = await GetDocumentForUrlAsync(url);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "1999",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "2000",
                ["Venue.Name"] = "Club",
                ["Venue.Address"] = "123 Fake Street",
                ["Venue.Frequency"] = ((int)Frequency.Weekly).ToString(),
                ["Venue.VenueTypeId"] = "1"
            };
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var socialContextVenuesPage = await AssertAndFollowRedirect(result,
                GetPathForId(NotificationSubPaths.EditSocialContextVenues, notificationId));
            // Follow the redirect to see results table
            var resultDocument = await GetDocumentAsync(socialContextVenuesPage);
            // We can't pick based on id, as we don't know the id created
            var venueTextContent = resultDocument.GetElementById("social-context-venues-list")
                .TextContent;

            Assert.Contains("Club", venueTextContent);
            Assert.Contains("123 Fake Street", venueTextContent);
            Assert.Contains("Weekly", venueTextContent);
        }

        [Fact]
        public async Task PostNewVenueWithCurlyBracketsInComments_ReturnsValidationErrors()
        {
            // Arrange
            const int notificationId = Utilities.DRAFT_ID;
            var url = GetPathForId(NotificationSubPaths.AddSocialContextVenue, notificationId);
            var initialDocument = await GetDocumentForUrlAsync(url);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "1999",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "2000",
                ["Venue.Name"] = "Club",
                ["Venue.Address"] = "123 Fake Street",
                ["Venue.Frequency"] = ((int)Frequency.Weekly).ToString(),
                ["Venue.VenueTypeId"] = "1",
                ["Venue.Details"] = "{{XSS}}",
            };
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            
            resultDocument.AssertErrorSummaryMessage("Venue-Details",
                null,
                string.Format(ValidationMessages.InvalidCharacter, "Comments"));
            
            result.AssertValidationErrorResponse();
        }

        [Fact]
        public async Task PostNewVenueWithCurlyBracketsInVenueName_ReturnsValidationErrors()
        {
            // Arrange
            const int notificationId = Utilities.DRAFT_ID;
            var url = GetPathForId(NotificationSubPaths.AddSocialContextVenue, notificationId);
            var initialDocument = await GetDocumentForUrlAsync(url);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "1999",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "2000",
                ["Venue.Name"] = "{{XSS}}",
                ["Venue.Address"] = "123 Fake Street",
                ["Venue.Frequency"] = ((int)Frequency.Weekly).ToString(),
                ["Venue.VenueTypeId"] = "1",
                ["Venue.Details"] = "",
            };
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            
            resultDocument.AssertErrorSummaryMessage("Venue-Name",
                null,
                string.Format(ValidationMessages.InvalidCharacter, "Venue name"));

            result.AssertValidationErrorResponse();
        }
        [Fact]
        public async Task PostEditOfVenue_ReturnsSuccessAndAmendsResultInTable()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_VENUES;
            var editUrl = GetCurrentPathForId(notificationId) + VENUE_ID;

            var editPage = await Client.GetAsync(editUrl);
            var editDocument = await GetDocumentAsync(editPage);
            var venueHeadingBeforeChanges = editDocument.GetElementById($"venue-heading-{VENUE_ID}").TextContent;
            var venueBodyBeforeChanges = editDocument.GetElementById($"venue-body-{VENUE_ID}").TextContent;
            Assert.Contains("Test venue", venueHeadingBeforeChanges);
            Assert.Contains("Test address", venueBodyBeforeChanges);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "1999",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "2000",
                ["Venue.Name"] = "New venue",
                ["Venue.Address"] = "New address",
                ["Venue.VenueTypeId"] = "1"
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);

            // Assert
            var socialContextVenuesPage = await AssertAndFollowRedirect(result,
                GetPathForId(NotificationSubPaths.EditSocialContextVenues, notificationId));
            // Follow the redirect to see results table
            var resultDocument = await GetDocumentAsync(socialContextVenuesPage);
            var venueHeadingTextContent = resultDocument.GetElementById($"venue-heading-{VENUE_ID}").TextContent;
            var venueBodyTextContent = resultDocument.GetElementById($"venue-body-{VENUE_ID}").TextContent;

            Assert.Contains("New venue", venueHeadingTextContent);
            Assert.Contains("New address", venueBodyTextContent);
        }

        [Fact]
        public async Task PostEditOfVenueWithMissingFields_ReturnsAllRequiredValidationErrors()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_VENUES;
            var editUrl = GetCurrentPathForId(notificationId) + VENUE_ID;
            var editDocument = await GetDocumentForUrlAsync(editUrl);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "",
                ["FormattedDateFrom.Month"] = "",
                ["FormattedDateFrom.Year"] = "",
                ["Venue.Name"] = "",
                ["Venue.Address"] = "",
                ["Venue.VenueTypeId"] = ""
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);
            var resultDocument = await GetDocumentAsync(result);

            // Assert
            result.AssertValidationErrorResponse();

            resultDocument.AssertErrorSummaryMessage("Venue",
                null,
                "Please supply at least one of venue type, name, address, postcode or comments");
        }

        [Fact]
        public async Task PostEditWithDateFromBeforeDateTo_ReturnsErrors()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_VENUES;
            var editUrl = GetCurrentPathForId(notificationId) + VENUE_ID;
            var editDocument = await GetDocumentForUrlAsync(editUrl);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "2000",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "1999",
                ["Venue.Name"] = "New venue",
                ["Venue.Address"] = "New address",
                ["Venue.VenueTypeId"] = "1"
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);
            var resultDocument = await GetDocumentAsync(result);

            // Assert
            result.AssertValidationErrorResponse();
            resultDocument.AssertErrorSummaryMessage(
                "Venue-DateTo",
                "date-to",
                ValidationMessages.VenueDateToShouldBeLaterThanDateFrom);
        }

        [Fact]
        public async Task GetEditForMissingVenue_ReturnsNotFound()
        {
            // Arrange
            const int notificationId = Utilities.DRAFT_ID;
            var editUrl = GetCurrentPathForId(notificationId) + VENUE_ID;

            // Act
            var editPage = await Client.GetAsync(editUrl);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, editPage.StatusCode);
        }

        [Fact]
        public async Task PostDelete_ReturnsSuccessAndRemovesResult()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_VENUES;
            var editUrl = GetCurrentPathForId(notificationId) + VENUE_TO_DELETE_ID;
            var editDocument = await GetDocumentForUrlAsync(editUrl);

            // Act
            var formData = new Dictionary<string, string> { };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl, "Delete");

            // Assert;
            var socialContextVenuesPage = await AssertAndFollowRedirect(result,
                GetPathForId(NotificationSubPaths.EditSocialContextVenues, notificationId));
            // Follow the redirect to see results table
            var resultDocument = await GetDocumentAsync(socialContextVenuesPage);
            Assert.Null(resultDocument.GetElementById($"social-context-venue-{VENUE_TO_DELETE_ID}"));
        }

        [Fact]
        public async Task ValidateSocialContextDates_ReturnsErrorIfDateToBeforeDateFrom()
        {
            // Arrange
            var initialPage = await Client.GetAsync(GetCurrentPathForId(Utilities.NOTIFICATION_WITH_VENUES) + VENUE_ID);
            var initialDocument = await GetDocumentAsync(initialPage);
            var keyValuePairs = new DatesValidationModel
            {
                KeyValuePairs = new List<Dictionary<string, string>>()
                {
                    new Dictionary<string, string>()
                    {
                        {"key", "DateFrom"},
                        {"day", "1"},
                        {"month", "1"},
                        {"year", "2000"}
                    },
                    new Dictionary<string, string>()
                    {
                        {"key", "DateTo"},
                        {"day", "1"},
                        {"month", "1"},
                        {"year", "1999"}
                    }
                }
            };

            // Act
            var response = await Client.SendVerificationPostAsync(
                initialPage,
                initialDocument,
                GetCurrentPathForId(Utilities.NOTIFICATION_WITH_VENUES) + VENUE_ID + "/ValidateSocialContextDates",
                keyValuePairs);

            // Assert check just response.Content
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains(ValidationMessages.VenueDateToShouldBeLaterThanDateFrom, result);
        }

        [Fact]
        public async Task ValidateSocialContextDatesRequest_ReturnsBadRequestIfDateInvalid()
        {
            // Arrange
            var initialPage = await Client.GetAsync(GetCurrentPathForId(Utilities.NOTIFICATION_WITH_VENUES) + VENUE_ID);
            var initialDocument = await GetDocumentAsync(initialPage);
            var keyValuePairs = new DatesValidationModel
            {
                KeyValuePairs = new List<Dictionary<string, string>>()
                {
                    new Dictionary<string, string>()
                    {
                        {"key", "DateFrom"},
                        {"day", "1"},
                        {"month", "1"},
                        {"year", "2000"}
                    },
                    new Dictionary<string, string>()
                    {
                        {"key", "DateTo"},
                        {"day", "1"},
                        {"month", "1"},
                        {"year", "1999901"}
                    }
                }
            };

            // Act
            var response = await Client.SendVerificationPostAsync(
                initialPage,
                initialDocument,
                GetCurrentPathForId(Utilities.NOTIFICATION_WITH_VENUES) + VENUE_ID + "/ValidateSocialContextDates",
                keyValuePairs);

            // Assert check just response.Content
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RedirectsToOverviewWithCorrectAnchorFragment_ForNotified()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var notificationSubPath = NotificationSubPaths.EditSocialContextVenues;
            var url = GetPathForId(notificationSubPath, id);
            var document = await GetDocumentForUrlAsync(url);

            // Act
            var result = await Client.SendPostFormWithData(document, null, url);

            // Assert
            var sectionAnchorId = OverviewSubPathToAnchorMap.GetOverviewAnchorId(notificationSubPath);
            result.AssertRedirectTo($"/Notifications/{id}#{sectionAnchorId}");
        }

        [Fact]
        public async Task NotifiedPageHasReturnLinkToOverview()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var notificationSubPath = NotificationSubPaths.EditSocialContextVenues;
            var url = GetPathForId(notificationSubPath, id);

            // Act
            var document = await GetDocumentForUrlAsync(url);

            // Assert
            var overviewLink = RouteHelper.GetNotificationOverviewPathWithSectionAnchor(id, notificationSubPath);
            Assert.NotNull(document.QuerySelector($"a[href='{overviewLink}']"));
        }
    }
}
