﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using ntbs_service.DataAccess;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Pages.Search;
using ntbs_service.Services;
using ntbs_service_unit_tests.Helpers;
using Xunit;

namespace ntbs_service_unit_tests.Pages
{
    public class SearchPageTest
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly Mock<IReferenceDataRepository> _mockReferenceDataRepository;
        private readonly Mock<ISearchService> _mockSearchService;
        private readonly Mock<IAuthorizationService> _mockAuthorizationService;
        private readonly Mock<ILegacySearchService> _mockLegacySearchService;
        private readonly Mock<IUserService> _mockUserService;
        public SearchPageTest()
        {
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockSearchService = new Mock<ISearchService>();
            _mockAuthorizationService = new Mock<IAuthorizationService>();
            _mockReferenceDataRepository = new Mock<IReferenceDataRepository>();
            _mockLegacySearchService = new Mock<ILegacySearchService>();
            _mockUserService = new Mock<IUserService>();
        }

        [Fact]
        public async Task OnGetAsync_PopulatesPageModel_WithSearchResults()
        {
            // Arrange
            IList<Sex> sexes = new List<Sex>();
            var sexList = Task.FromResult(sexes);
            _mockReferenceDataRepository.Setup(s => s.GetAllSexesAsync()).Returns(sexList);

            IList<Country> countries = new List<Country>();
            var countrySelectList = Task.FromResult(countries);
            _mockReferenceDataRepository.Setup(s => s.GetAllCountriesAsync()).Returns(countrySelectList);

            IList<TBService> tbServices = new List<TBService>();
            var tbServiceList = Task.FromResult(tbServices);
            _mockReferenceDataRepository.Setup(s => s.GetAllActiveTbServicesAsync()).Returns(tbServiceList);

            _mockNotificationRepository
                .Setup(s => s.GetQueryableNotificationByStatus(It.IsAny<List<NotificationStatus>>()))
                .Returns(new List<Notification> { new Notification() { NotificationId = 1 } }.AsQueryable());

            var unionAndPaginateResult = Task.FromResult(GetNotificationIdsAndCount());
            _mockSearchService.Setup(s => s.OrderAndPaginateQueryableAsync(It.IsAny<INtbsSearchBuilder>(),
                It.IsAny<PaginationParameters>(), It.IsAny<ClaimsPrincipal>())).Returns(unionAndPaginateResult);

            var notifications = Task.FromResult(GetNotificationBanners());
            _mockNotificationRepository.Setup(s => s.GetNotificationBannerModelsByIdsAsync(It.IsAny<List<int>>())).Returns(notifications);

            var legacyNotificationsAndCount = Task.FromResult(GetLegacyNotificationBanner());
            _mockLegacySearchService
                .Setup(s => s.SearchAsync(It.IsAny<ILegacySearchBuilder>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<ClaimsPrincipal>()))
                .Returns(legacyNotificationsAndCount);

            _mockAuthorizationService.Setup(s =>
                    s.SetFullAccessOnNotificationBannersAsync(It.IsAny<IEnumerable<NotificationBannerModel>>(),
                        It.IsAny<ClaimsPrincipal>()))
                .Verifiable();


            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);
            var pageContext = new PageContext(actionContext);

            var pageModel = new IndexModel(
                _mockNotificationRepository.Object,
                _mockSearchService.Object,
                _mockAuthorizationService.Object,
                _mockReferenceDataRepository.Object,
                _mockLegacySearchService.Object,
                _mockUserService.Object,
                new Mock<IUserHelper>().Object)
            {
                SearchParameters = new SearchParameters(),
                PageContext = pageContext
            };
            pageModel.MockOutSession(httpContext);

            // Act
            await pageModel.OnGetAsync(1);

            // Assert
            var results = Assert.IsAssignableFrom<PaginatedList<NotificationBannerModel>>(pageModel.SearchResults);
            Assert.True(results.Count == 3);
            Assert.Equal("Bob Ross", results[0].Name);
            Assert.Equal("Jack Jill", results[1].Name);
            Assert.Equal("Luke Arrow", results[2].Name);
        }

        public (IList<int> notificationIds, int count) GetNotificationIdsAndCount()
        {
            return (notificationIds: new List<int> { 1, 2 }, count: 2);
        }

        public IEnumerable<NotificationBannerModel> GetNotificationBanners()
        {
            return new List<NotificationBannerModel> {
                new NotificationBannerModel { Name = "Bob Ross", NotificationId = "1", SortByDate = DateTime.Now },
                new NotificationBannerModel { Name = "Jack Jill", NotificationId = "2", SortByDate = DateTime.Now.AddDays(-1) }
                };
        }

        public (IEnumerable<NotificationBannerModel>, int) GetLegacyNotificationBanner()
        {
            return (new List<NotificationBannerModel> { new NotificationBannerModel { Name = "Luke Arrow", NotificationId = "134-1", SortByDate = DateTime.Now.AddDays(-2) } }, 1);
        }

        public IEnumerable<NotificationBannerModel> GetAuthorisedBanners()
        {
            return new List<NotificationBannerModel> {
                new NotificationBannerModel { Name = "Bob Ross", NotificationId = "1", SortByDate = DateTime.Now },
                new NotificationBannerModel { Name = "Jack Jill", NotificationId = "2", SortByDate = DateTime.Now.AddDays(-1) },
                new NotificationBannerModel { Name = "Luke Arrow", NotificationId = "134-1", SortByDate = DateTime.Now.AddDays(-2) }
                };
        }
    }
}
