﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using ntbs_service.DataAccess;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Properties;
using ntbs_service.Services;
using Xunit;

namespace ntbs_service_unit_tests.Services
{
    public class UserServiceTest
    {
        private const string NationalTeam = "NTS";
        private const string ServicePrefix = "Service";
        private readonly IUserService _service;
        private readonly AdOptions _options = new AdOptions
        {
            NationalTeamAdGroup = NationalTeam,
            ServiceGroupAdPrefix = ServicePrefix
        };

        private readonly Mock<IReferenceDataRepository> _mockReferenceDataRepository;
        private readonly Mock<ClaimsPrincipal> _mockUser;

        public UserServiceTest()
        {
            _mockReferenceDataRepository = new Mock<IReferenceDataRepository>();
            var mockOptionsMonitor = new Mock<IOptionsMonitor<AdOptions>>();
            var mockUserRepository = new Mock<IUserRepository>();
            mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(_options);
            _service = new UserService(_mockReferenceDataRepository.Object, mockUserRepository.Object, mockOptionsMonitor.Object);

            _mockUser = new Mock<ClaimsPrincipal>();
        }

        [Fact]
        public async Task GetUserPermissionsFilter_ReturnsExpectedFilter_ForNationalUser()
        {
            // Arrange
            var allPhec = new List<PHEC> {new PHEC {Code = "PHEC001"}};
            _mockReferenceDataRepository.Setup(r => r.GetAllPhecs()).ReturnsAsync(allPhec);
            var claim = new Claim("User", NationalTeam);
            SetupClaimMocking(claim);

            // Act
            var result = await _service.GetUserPermissionsFilterAsync(_mockUser.Object);

            // Assert
            Assert.Empty(result.IncludedTBServiceCodes);
            Assert.Equal(allPhec.Select(p => p.Code), result.IncludedPHECCodes);
            Assert.Equal(UserType.NationalTeam, result.Type);
        }

        [Fact]
        public async Task GetUserPermissionsFilter_ReturnsExpectedFilter_ForNhsUser()
        {
            // Arrange
            const string serviceAdGroup = ServicePrefix + "Ashford";
            const string code = "TBS0008";

            var claim = new Claim("User", serviceAdGroup);
            SetupClaimMocking(claim);
            var serviceCodeList = new List<string> { code };
            _mockReferenceDataRepository.Setup(c => c.GetTbServiceCodesMatchingRolesAsync(It.Is<IEnumerable<string>>(l => l.Contains(serviceAdGroup))))
                .Returns(Task.FromResult(serviceCodeList));
            _mockReferenceDataRepository.Setup(c => c.GetPhecCodesMatchingRolesAsync(It.Is<IEnumerable<string>>(l => l.Contains(serviceAdGroup))))
                .Returns(Task.FromResult(new List<string>()));

            // Act
            var result = await _service.GetUserPermissionsFilterAsync(_mockUser.Object);

            // Assert
            Assert.Empty(result.IncludedPHECCodes);
            Assert.Single(result.IncludedTBServiceCodes);
            Assert.Equal(code, result.IncludedTBServiceCodes.First());
            Assert.Equal(UserType.ServiceOrPhecUser, result.Type);
        }

        [Fact]
        public async Task GetUserPermissionsFilter_ReturnsExpectedFilter_ForPheUser()
        {
            // Arrange
            const string adGroup = "SoE";
            const string code = "E45000019";

            var claim = new Claim("User", adGroup);
            SetupClaimMocking(claim);
            var phecCodeList = new List<string> { code };
            _mockReferenceDataRepository.Setup(c => c.GetPhecCodesMatchingRolesAsync(It.Is<IEnumerable<string>>(l => l.Contains(adGroup))))
                .Returns(Task.FromResult(phecCodeList));
            _mockReferenceDataRepository.Setup(c => c.GetTbServiceCodesMatchingRolesAsync(It.Is<IEnumerable<string>>(l => l.Contains(adGroup))))
                .Returns(Task.FromResult(new List<string>()));

            // Act
            var result = await _service.GetUserPermissionsFilterAsync(_mockUser.Object);

            // Assert
            Assert.Empty(result.IncludedTBServiceCodes);
            Assert.Single(result.IncludedPHECCodes);
            Assert.Equal(code, result.IncludedPHECCodes.First());
            Assert.Equal(UserType.ServiceOrPhecUser, result.Type);
        }

        [Fact]
        public async Task GetDefaultTbService_ReturnsMatchingService_ForNationalUser()
        {
            // Arrange
            const string serviceAdGroup = ServicePrefix + "Ashford";
            var tbService = new TBService { ServiceAdGroup = serviceAdGroup, Code = "TBS0008" };

            var claim = new Claim("User", NationalTeam);
            SetupClaimMocking(claim);
            var mockTbServiceQuery = new List<TBService> { tbService }.AsQueryable().BuildMock();
            _mockReferenceDataRepository.Setup(c => c.GetActiveTbServicesOrderedByNameQueryable())
                .Returns(mockTbServiceQuery.Object);
            // Act
            var result = await _service.GetDefaultTbService(_mockUser.Object);

            // Assert
            Assert.Equal(tbService, result);
        }

        [Fact]
        public async Task GetDefaultTbService_ReturnsMatchingService_ForNhsUser()
        {
            // Arrange
            const string serviceAdGroup = ServicePrefix + "Ashford";
            var tbService = new TBService() { ServiceAdGroup = serviceAdGroup, Code = "TBS0008" };

            var claim = new Claim("User", serviceAdGroup);
            SetupClaimMocking(claim);
            var mockTbServiceQuery = new List<TBService> { tbService }.AsQueryable().BuildMock();
            _mockReferenceDataRepository.Setup(c => c.GetDefaultTbServicesForUserQueryable(It.Is<IEnumerable<string>>(l => l.Contains(serviceAdGroup))))
                .Returns(mockTbServiceQuery.Object);

            // Act
            var result = await _service.GetDefaultTbService(_mockUser.Object);

            // Assert
            Assert.Equal(tbService, result);
        }

        [Fact]
        public async Task GetDefaultTbService_ReturnsMatchingService_ForPheUser()
        {
            // Arrange
            const string adGroup = "SoE";
            const string serviceAdGroup = ServicePrefix + "Ashford";
            var tbService = new TBService() { ServiceAdGroup = serviceAdGroup, Code = "TBS0008" };

            var claim = new Claim("User", adGroup);
            SetupClaimMocking(claim);
            var mockTbServiceQuery = new List<TBService> { tbService }.AsQueryable().BuildMock();
            _mockReferenceDataRepository.Setup(c => c.GetDefaultTbServicesForUserQueryable(It.Is<IEnumerable<string>>(l => l.Contains(adGroup))))
                .Returns(mockTbServiceQuery.Object);

            // Act
            var result = await _service.GetDefaultTbService(_mockUser.Object);

            // Assert
            Assert.Equal(tbService, result);
        }

        [Theory]
        [MemberData(nameof(FormattedNamePairs))]
        public void GetUserDisplayName_ReturnsFormattedIdentityName_WhenNotAnEmail(string identityName,
            string expectedName)
        {
            var mockIdentity = new Mock<IIdentity>();
            mockIdentity.Setup(i => i.Name).Returns(identityName);
            _mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);

            // Act
            var result = _service.GetUserDisplayName(_mockUser.Object);

            // Assert
            Assert.Equal(expectedName, result);
            _mockUser.Verify(u => u.Claims, Times.Never);
        }

        [Theory]
        [MemberData(nameof(FormattedNamePairs))]
        public void GetUserDisplayName_ReturnsFormattedClaimName_WhenIdentityNameIsEmail(string claimName,
            string expectedName)
        {
            var mockIdentity = new Mock<IIdentity>();
            mockIdentity.Setup(i => i.Name).Returns("name@example.com");
            _mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
            _mockUser.Setup(u => u.Claims).Returns(new[] { new Claim("name", claimName, null) });

            // Act
            var result = _service.GetUserDisplayName(_mockUser.Object);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Theory]
        [MemberData(nameof(FormattedNamePairs))]
        public void GetUserDisplayName_ReturnsFormattedClaimName_WhenIdentityNameIsNull(string claimName,
            string expectedName)
        {
            var mockIdentity = new Mock<IIdentity>();
            mockIdentity.Setup(i => i.Name).Returns((string) null);
            _mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
            _mockUser.Setup(u => u.Claims).Returns(new[] { new Claim("name", claimName, null) });

            // Act
            var result = _service.GetUserDisplayName(_mockUser.Object);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Theory]
        [MemberData(nameof(NoNameClaimLists))]
        public void GetUserDisplayName_ReturnsFormattedIdentityName_WhenNoNameClaim(IEnumerable<Claim> claims)
        {
            const string identityName = "perry.cox@example.com";

            var mockIdentity = new Mock<IIdentity>();
            mockIdentity.Setup(i => i.Name).Returns(identityName);
            _mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
            _mockUser.Setup(u => u.Claims).Returns(claims);

            // Act
            var result = _service.GetUserDisplayName(_mockUser.Object);

            // Assert
            Assert.Equal(identityName, result);
        }

        [Theory]
        [MemberData(nameof(NoNameClaimLists))]
        public void GetUserDisplayName_ReturnsNullIdentityName_WhenNoNameClaim(IEnumerable<Claim> claims)
        {
            const string identityName = null;

            var mockIdentity = new Mock<IIdentity>();
            mockIdentity.Setup(i => i.Name).Returns(identityName);
            _mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
            _mockUser.Setup(u => u.Claims).Returns(claims);

            // Act
            var result = _service.GetUserDisplayName(_mockUser.Object);

            // Assert
            Assert.Equal(identityName, result);
        }

        public static IEnumerable<object[]> NoNameClaimLists => new[]
        {
            new object[] { Enumerable.Empty<Claim>() },
            new object[] { new [] { new Claim("job-title", "Doctor", null) } }
        };

        public static IEnumerable<object[]> FormattedNamePairs => new[]
        {
            new object[] { "John Dorian", "John Dorian" },
            new object[] { "    Christopher Turk   ", "Christopher Turk" },
            new object[] { "    Elliot Reid  (Sacred Heart Hospital)", "Elliot Reid" },
        };

        private void SetupClaimMocking(Claim claim)
        {
            _mockUser.Setup(u => u.FindAll(It.IsAny<Predicate<Claim>>())).Returns(new List<Claim> { claim });
            _mockUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(role => claim.Value == role);
        }
    }
}
