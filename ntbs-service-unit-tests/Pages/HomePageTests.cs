﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using ntbs_service.DataAccess;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Pages;
using ntbs_service.Services;
using ntbs_service_unit_tests.Helpers;
using Xunit;

namespace ntbs_service_unit_tests.Pages
{
    public class HomePageTests : IClassFixture<HomePageFixture>
    {
        private readonly HomePageFixture _homePageFixture;
        private readonly INotificationRepository _notificationRepository;

        private readonly Mock<IAuthorizationService> _mockAuthorizationService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAlertRepository> _mockAlertRepository;
        private readonly Mock<IHomepageKpiService> _mockHomepageKpiService;

        private readonly HomepageKpi mockHomepageKpiWithPhec = new HomepageKpi
        {
            Code = "PHEC001",
            PercentPositive = 17,
            PercentHivOffered = 25,
            PercentResistant = 11,
            PercentTreatmentDelay = 14
        };

        private readonly HomepageKpi mockHomepageKpiWithTbService = new HomepageKpi
        {
            Code = "TB001",
            PercentPositive = 15,
            PercentHivOffered = 27,
            PercentResistant = 12,
            PercentTreatmentDelay = 13
        };

        public HomePageTests(HomePageFixture homePageFixture)
        {
            _mockAuthorizationService = new Mock<IAuthorizationService>();
            _mockUserService = new Mock<IUserService>();
            _mockAlertRepository = new Mock<IAlertRepository>();
            _mockHomepageKpiService = new Mock<IHomepageKpiService>();
            SetUpMocks();

            _homePageFixture = homePageFixture;
            _notificationRepository = new NotificationRepository(homePageFixture.Context);
        }

        private void SetUpMocks()
        {
            _mockAuthorizationService
                .Setup(s => s.FilterNotificationsByUserAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IQueryable<Notification>>()))
                .Returns((ClaimsPrincipal user, IQueryable<Notification> notifications) => Task.FromResult(notifications));
            _mockAuthorizationService
                .Setup(s => s.FilterNotificationsByUserAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IQueryable<Notification>>()))
                .Returns((ClaimsPrincipal user, IQueryable<Notification> notifications) => Task.FromResult(notifications));
            _mockAuthorizationService
                .Setup(s => s.FilterAlertsForUserAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IList<AlertWithTbServiceForDisplay>>()))
                .Returns((ClaimsPrincipal user, IList<AlertWithTbServiceForDisplay> alerts) => Task.FromResult(alerts));

            _mockUserService.Setup(s => s.GetUserType(It.IsAny<ClaimsPrincipal>())).Returns(UserType.NationalTeam);
            _mockUserService.Setup(s => s.GetPhecCodesAsync(It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.FromResult(new List<string> { mockHomepageKpiWithPhec.Code } as IEnumerable<string>));
            _mockHomepageKpiService.Setup(s => s.GetKpiForPhec(new List<string> { mockHomepageKpiWithPhec.Code }))
                .Returns(Task.FromResult(new List<HomepageKpi> { mockHomepageKpiWithPhec } as IEnumerable<HomepageKpi>));
            _mockHomepageKpiService.Setup(s => s.GetKpiForTbService(new List<string> { mockHomepageKpiWithTbService.Code }))
                .Returns(Task.FromResult(new List<HomepageKpi> { mockHomepageKpiWithTbService } as IEnumerable<HomepageKpi>));
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("2021-04-01", "2021-04-15", "2021-04-30")]
        public async Task OnGetAsync_PopulatesPageModel_WithRecentNotifications(string submissionDate1,
            string submissionDate2,
            string submissionDate3)
        {
            // Arrange
            var recents = new List<Notification>
            {
                new Notification
                {
                    PatientDetails = new PatientDetails{ GivenName = "Alice" },
                    NotificationStatus = NotificationStatus.Notified,
                    SubmissionDate = submissionDate1 == null ? (DateTime?)null : DateTime.Parse(submissionDate1),
                    NotificationDate = DateTime.Parse("2021-04-01")
                },
                new Notification
                {
                    PatientDetails = new PatientDetails{ GivenName = "Bob" },
                    NotificationStatus = NotificationStatus.Notified,
                    SubmissionDate = submissionDate1 == null ? (DateTime?)null : DateTime.Parse(submissionDate2),
                    NotificationDate = DateTime.Parse("2021-04-30")
                },
                new Notification
                {
                    PatientDetails = new PatientDetails{ GivenName = "Charlie" },
                    NotificationStatus = NotificationStatus.Notified,
                    SubmissionDate = submissionDate1 == null ? (DateTime?)null : DateTime.Parse(submissionDate3),
                    NotificationDate = DateTime.Parse("2021-04-15")
                }
            };

            await _homePageFixture.Context.AddRangeAsync(recents);
            await _homePageFixture.Context.SaveChangesAsync();

            var pageModel = new IndexModel(_notificationRepository,
                _mockAlertRepository.Object,
                _mockAuthorizationService.Object,
                _mockUserService.Object,
                _mockHomepageKpiService.Object);
            pageModel.MockOutSession();

            // Act
            await pageModel.OnGetAsync();
            var resultRecents = Assert.IsAssignableFrom<List<Notification>>(pageModel.RecentNotifications);

            // Assert
            Assert.Equal(new[] { "Bob", "Charlie", "Alice" }, resultRecents.Select(n => n.PatientDetails.GivenName));
            _homePageFixture.Context.RemoveRange(recents);
        }

        [Fact]
        public async Task OnGetAsync_PopulatesPageModel_WithDraftNotifications()
        {
            // Arrange
            var drafts = new List<Notification>
            {
                new Notification
                {
                    PatientDetails = new PatientDetails{ GivenName = "Dave" },
                    NotificationStatus = NotificationStatus.Draft,
                    SubmissionDate = DateTime.Parse("2021-04-01")
                },
                new Notification
                {
                    PatientDetails = new PatientDetails{ GivenName = "Eve" },
                    NotificationStatus = NotificationStatus.Draft,
                    SubmissionDate = DateTime.Parse("2021-04-30")
                },
                new Notification
                {
                    PatientDetails = new PatientDetails{ GivenName = "Frank" },
                    NotificationStatus = NotificationStatus.Draft,
                    SubmissionDate = DateTime.Parse("2021-04-15")
                }
            };

            await _homePageFixture.Context.AddRangeAsync(drafts);
            await _homePageFixture.Context.SaveChangesAsync();

            var pageModel = new IndexModel(_notificationRepository,
                _mockAlertRepository.Object,
                _mockAuthorizationService.Object,
                _mockUserService.Object,
                _mockHomepageKpiService.Object);
            pageModel.MockOutSession();

            // Act
            await pageModel.OnGetAsync();
            var resultDrafts = Assert.IsAssignableFrom<List<Notification>>(pageModel.DraftNotifications);

            // Assert
            Assert.Equal(new[] { "Eve", "Frank", "Dave" }, resultDrafts.Select(n => n.PatientDetails.GivenName));
            _homePageFixture.Context.RemoveRange(drafts);
        }

        [Fact]
        public async Task OnGetAsync_PopulatesPageModel_WithAlerts()
        {
            // Arrange
            var alerts = new List<AlertWithTbServiceForDisplay> { new AlertWithTbServiceForDisplay { AlertId = 101 } };
            _mockAlertRepository.Setup(s => s.GetOpenAlertsByTbServiceCodesAsync(It.IsAny<IEnumerable<string>>()))
                .Returns(Task.FromResult(alerts));
            _mockUserService.Setup(s => s.GetUserType(It.IsAny<ClaimsPrincipal>())).Returns(UserType.PheUser);

            var pageModel = new IndexModel(_notificationRepository,
                _mockAlertRepository.Object,
                _mockAuthorizationService.Object,
                _mockUserService.Object,
                _mockHomepageKpiService.Object);
            pageModel.MockOutSession();

            // Act
            await pageModel.OnGetAsync();
            var resultAlerts = Assert.IsAssignableFrom<List<AlertWithTbServiceForDisplay>>(pageModel.Alerts);

            // Assert
            Assert.Collection(resultAlerts, alert => alert.AlertId = 101);
        }

        [Fact]
        public async Task OnGetAsync_PopulatesHomepageKpisDetailsWithPhecCodes_WhenUserIsPheUser()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserType(It.IsAny<ClaimsPrincipal>())).Returns(UserType.PheUser);
            _mockUserService.Setup(s => s.GetPhecCodesAsync(It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.FromResult(new List<string> { mockHomepageKpiWithPhec.Code } as IEnumerable<string>));

            var pageModel = new IndexModel(_notificationRepository,
                _mockAlertRepository.Object,
                _mockAuthorizationService.Object,
                _mockUserService.Object,
                _mockHomepageKpiService.Object);
            pageModel.MockOutSession();

            // Act
            await pageModel.OnGetAsync();
            var phecCodes = Assert.IsAssignableFrom<SelectList>(pageModel.KpiFilter);
            var homepageKpiDetails = Assert.IsAssignableFrom<List<HomepageKpi>>(pageModel.HomepageKpiDetails);

            Assert.True(phecCodes.Count() == 1);
            Assert.True(homepageKpiDetails.Count == 1);
            Assert.True(homepageKpiDetails[0] == mockHomepageKpiWithPhec);
        }

        [Fact]
        public async Task OnGetAsync_PopulatesHomepageKpisDetailsWithTbServiceCodes_WhenUserIsNhsUser()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserType(It.IsAny<ClaimsPrincipal>())).Returns(UserType.NhsUser);
            var mockTbService = new TBService() { Code = mockHomepageKpiWithTbService.Code };
            _mockUserService.Setup(s => s.GetTbServicesAsync(It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.FromResult(new List<TBService> { mockTbService } as IEnumerable<TBService>));

            var pageModel = new IndexModel(_notificationRepository,
                _mockAlertRepository.Object,
                _mockAuthorizationService.Object,
                _mockUserService.Object,
                _mockHomepageKpiService.Object);
            pageModel.MockOutSession();

            // Act
            await pageModel.OnGetAsync();
            var phecCodes = Assert.IsAssignableFrom<SelectList>(pageModel.KpiFilter);
            var homepageKpiDetails = Assert.IsAssignableFrom<List<HomepageKpi>>(pageModel.HomepageKpiDetails);

            Assert.True(phecCodes.Count() == 1);
            Assert.True(homepageKpiDetails.Count == 1);
            Assert.True(homepageKpiDetails[0] == mockHomepageKpiWithTbService);
        }
    }
}
