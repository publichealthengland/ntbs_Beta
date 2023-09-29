﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ntbs_integration_tests.Helpers;
using ntbs_service;
using ntbs_service.Helpers;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.Validations;
using Xunit;

namespace ntbs_integration_tests.NotificationPages
{
    public class SocialContextAddressEditPageTests : TestRunnerNotificationBase
    {
        private const int ADDRESS_ID = 10;
        private const int ADDRESS_TO_DELETE_ID = 11;
        private const int ADDRESS_ID_WITH_CURLY_BRACKETS = 12;
        protected override string NotificationSubPath => NotificationSubPaths.EditSocialContextAddressSubPath;

        public SocialContextAddressEditPageTests(NtbsWebApplicationFactory<EntryPoint> factory) : base(factory)
        {
        }

        public static IList<Notification> GetSeedingNotifications()
        {
            return new List<Notification>
            {
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_WITH_ADDRESSES,
                    NotificationStatus = NotificationStatus.Notified,
                    SocialContextAddresses = new List<SocialContextAddress> () {
                        new SocialContextAddress {
                            SocialContextAddressId = ADDRESS_ID,
                            DateFrom = new DateTime(2012, 1, 1),
                            DateTo = new DateTime(2013, 1, 1),
                            Address = "Softwire London",
                            Postcode = "NW5 1TL",
                            Details = "Head Office"
                        },
                        new SocialContextAddress {
                            SocialContextAddressId = ADDRESS_TO_DELETE_ID,
                            DateFrom = new DateTime(2012, 1, 1),
                            DateTo = new DateTime(2013, 1, 1),
                            Address = "Softwire Manchester",
                            Postcode = "M4 4BF",
                            Details = "Regional Office"
                        },
                    }
                },
                new Notification
                {
                    NotificationId = Utilities.NOTIFICATION_ID_WITH_CURLY_BRACKETS_IN_SOCIALCONTEXTADDRESS,
                    NotificationStatus = NotificationStatus.Notified,
                    SocialContextAddresses = new List<SocialContextAddress>
                    {
                        new SocialContextAddress
                        {
                            SocialContextAddressId = ADDRESS_ID_WITH_CURLY_BRACKETS,
                            Details = "{{abc}}"
                        }
                    }
                }
            };
        }

        [Fact]
        public async Task PostNewAddress_ReturnsSuccessAndAddsResultToTable()
        {
            // Arrange
            const int notificationId = Utilities.DRAFT_ID;
            var url = GetPathForId(NotificationSubPaths.AddSocialContextAddress, notificationId);
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
                ["Address.Address"] = "123 Fake Street",
                ["Address.Postcode"] = "M4 4BF",
                ["Address.Details"] = "Fake Office"
            };
            var result = await Client.SendPostFormWithData(initialDocument, formData, url);

            // Assert
            var socialContextAddressesPage = await AssertAndFollowRedirect(result, GetPathForId(NotificationSubPaths.EditSocialContextAddresses, notificationId));
            // Follow the redirect to see results table
            var resultDocument = await GetDocumentAsync(socialContextAddressesPage);
            // We can't pick based on id, as we don't know the id created
            var addressTextContent = resultDocument.GetElementById("social-context-addresses-list")
                .TextContent;

            Assert.Contains("123 Fake Street", addressTextContent);
            Assert.Contains("M4 4BF", addressTextContent);
            Assert.Contains("Fake Office", addressTextContent);
        }

        [Fact]
        public async Task PostEditOfAddress_ReturnsSuccessAndAmendsResultInTable()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_ADDRESSES;
            var editUrl = GetCurrentPathForId(notificationId) + ADDRESS_ID;

            var editPage = await Client.GetAsync(editUrl);
            var editDocument = await GetDocumentAsync(editPage);
            var addressBodyBeforeChanges = editDocument.GetElementById($"address-body-{ADDRESS_ID}").TextContent;
            Assert.Contains("Softwire London", addressBodyBeforeChanges);
            Assert.Contains("NW5 1TL", addressBodyBeforeChanges);
            Assert.Contains("Head Office", addressBodyBeforeChanges);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "1999",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "2000",
                ["Address.Address"] = "New address",
                ["Address.Postcode"] = "M4 4BF",
                ["Address.Details"] = "New Head Office"
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);

            // Assert
            var socialContextAddressesPage = await AssertAndFollowRedirect(result, GetPathForId(NotificationSubPaths.EditSocialContextAddresses, notificationId));
            // Follow the redirect to see results table
            var resultDocument = await GetDocumentAsync(socialContextAddressesPage);
            var addressBodyTextContent = resultDocument.GetElementById($"address-body-{ADDRESS_ID}").TextContent;

            Assert.Contains("New address", addressBodyTextContent);
            Assert.Contains("M4 4BF", addressBodyTextContent);
            Assert.Contains("New Head Office", addressBodyTextContent);
        }

        [Fact]
        public async Task PostEditOfAddressWithMissingFields_ReturnsAllRequiredValidationErrors()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_ADDRESSES;
            var editUrl = GetCurrentPathForId(notificationId) + ADDRESS_ID;
            var editDocument = await GetDocumentForUrlAsync(editUrl);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "",
                ["FormattedDateFrom.Month"] = "",
                ["FormattedDateFrom.Year"] = "",
                ["FormattedDateTo.Day"] = "",
                ["FormattedDateTo.Month"] = "",
                ["FormattedDateTo.Year"] = "",
                ["Address.Address"] = "",
                ["Address.Postcode"] = ""
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);
            var resultDocument = await GetDocumentAsync(result);

            // Assert
            result.AssertValidationErrorResponse();

            resultDocument.AssertErrorSummaryMessage("Address",
                null,
                "Please supply at least one of address, postcode or comments");
        }

        [Fact]
        public async Task PostEditWithDateFromBeforeDateTo_ReturnsErrors()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_ADDRESSES;
            var editUrl = GetCurrentPathForId(notificationId) + ADDRESS_ID;
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
                ["Address.Address"] = "New address",
                ["Address.Postcode"] = "M4 4BF"
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);
            var resultDocument = await GetDocumentAsync(result);

            // Assert
            result.AssertValidationErrorResponse();
            resultDocument.AssertErrorMessage("date-to", ValidationMessages.VenueDateToShouldBeLaterThanDateFrom);
        }

        [Fact]
        public async Task PostEditOfAddressWithCommentContainingCurlyBrackets_ReturnsValidationErrors()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_ADDRESSES;
            var editUrl = GetCurrentPathForId(notificationId) + ADDRESS_ID;

            var editPage = await Client.GetAsync(editUrl);
            var editDocument = await GetDocumentAsync(editPage);

            // Act
            var formData = new Dictionary<string, string>
            {
                ["FormattedDateFrom.Day"] = "1",
                ["FormattedDateFrom.Month"] = "1",
                ["FormattedDateFrom.Year"] = "1999",
                ["FormattedDateTo.Day"] = "1",
                ["FormattedDateTo.Month"] = "1",
                ["FormattedDateTo.Year"] = "2000",
                ["Address.Address"] = "New address",
                ["Address.Postcode"] = "M4 4BF",
                ["Address.Details"] = "{{XSS}}"
            };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl);

            // Assert
            var resultDocument = await GetDocumentAsync(result);
            
            
            resultDocument.AssertErrorSummaryMessage("Address-Details",
                null,
                string.Format(ValidationMessages.InvalidCharacter, "Comments"));
            

            result.AssertValidationErrorResponse();
        }
        [Fact]
        public async Task GetEditForMissingAddress_ReturnsNotFound()
        {
            // Arrange
            const int notificationId = Utilities.DRAFT_ID;
            var editUrl = GetCurrentPathForId(notificationId) + ADDRESS_ID;

            // Act
            var editPage = await Client.GetAsync(editUrl);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, editPage.StatusCode);
        }

        [Fact]
        public async Task GetEditOfAddress_RemovesCurlyBrackets()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_ID_WITH_CURLY_BRACKETS_IN_SOCIALCONTEXTADDRESS;
            var url = GetCurrentPathForId(notificationId) + ADDRESS_ID_WITH_CURLY_BRACKETS;
            // Act
            var document = await GetDocumentForUrlAsync(url);

            // Assert
            var detailsContainer = document.GetElementById("social-context-addresses-list").TextContent;
            var addressDetails = document.QuerySelector<IHtmlTextAreaElement>("#Address_Details").Value;

            Assert.DoesNotContain("{", detailsContainer);
            Assert.DoesNotContain("}", detailsContainer);
            Assert.Contains("abc", detailsContainer);
            
            Assert.DoesNotContain("{", addressDetails);
            Assert.DoesNotContain("}", addressDetails);
            Assert.Equal("abc", addressDetails);
        }

        [Fact]
        public async Task PostDelete_ReturnsSuccessAndRemovesResult()
        {
            // Arrange
            const int notificationId = Utilities.NOTIFICATION_WITH_ADDRESSES;
            var editUrl = GetCurrentPathForId(notificationId) + ADDRESS_TO_DELETE_ID;
            var editDocument = await GetDocumentForUrlAsync(editUrl);

            // Act
            var formData = new Dictionary<string, string> { };
            var result = await Client.SendPostFormWithData(editDocument, formData, editUrl, "Delete");

            // Assert
            var socialContextAddressesPage = await AssertAndFollowRedirect(result, GetPathForId(NotificationSubPaths.EditSocialContextAddresses, notificationId));
            // Follow the redirect to see results table
            var resultDocument = await GetDocumentAsync(socialContextAddressesPage);
            Assert.Null(resultDocument.GetElementById($"social-context-address-{ADDRESS_TO_DELETE_ID}"));
        }

        [Fact]
        public async Task ValidateSocialContextDates_ReturnsErrorIfDateToBeforeDateFrom()
        {
            // Arrange
            var initialPage = await Client.GetAsync(GetCurrentPathForId(Utilities.NOTIFICATION_WITH_ADDRESSES) + ADDRESS_ID);
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
                GetCurrentPathForId(Utilities.NOTIFICATION_WITH_ADDRESSES) + ADDRESS_ID + "/ValidateSocialContextDates",
                keyValuePairs);

            // Assert check just response.Content
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains(ValidationMessages.VenueDateToShouldBeLaterThanDateFrom, result);
        }

        [Fact]
        public async Task ValidateSocialContextDatesRequest_ReturnsBadRequestIfDateInvalid()
        {
            // Arrange
            var initialPage = await Client.GetAsync(GetCurrentPathForId(Utilities.NOTIFICATION_WITH_ADDRESSES) + ADDRESS_ID);
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
                        {"year", "20000301"}
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
                GetCurrentPathForId(Utilities.NOTIFICATION_WITH_ADDRESSES) + ADDRESS_ID + "/ValidateSocialContextDates",
                keyValuePairs);

            // Assert check just response.Content
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RedirectsToOverviewWithCorrectAnchorFragment_ForNotified()
        {
            // Arrange
            const int id = Utilities.NOTIFIED_ID;
            var notificationSubPath = NotificationSubPaths.EditSocialContextAddresses;
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
            var notificationSubPath = NotificationSubPaths.EditSocialContextAddresses;
            var url = GetPathForId(notificationSubPath, id);

            // Act
            var document = await GetDocumentForUrlAsync(url);

            // Assert
            var overviewLink = RouteHelper.GetNotificationOverviewPathWithSectionAnchor(id, notificationSubPath);
            Assert.NotNull(document.QuerySelector($"a[href='{overviewLink}']"));
        }
    }
}
