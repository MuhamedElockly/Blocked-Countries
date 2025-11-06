using System.Threading.Tasks;
using BlockedCountries.Business.Models;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using BlockedCountries.Data.Models;
using BlockedCountries.Data.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlockedCountries.Tests;

public class IpBlockingServiceTests
{
    private static HttpContext CreateHttpContext(string? userAgent = null)
    {
        var ctx = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(userAgent))
        {
            ctx.Request.Headers["User-Agent"] = userAgent;
        }
        return ctx;
    }

    [Fact]
    public async Task LookupIpAddressAsync_returns_failure_when_geo_returns_null()
    {
        var geo = new Mock<IGeolocationService>();
        geo.Setup(g => g.LookupIpAddressAsync(It.IsAny<string?>()))
           .ReturnsAsync((IpLookupResponse?)null);

        var svc = new IpBlockingService(
            geo.Object,
            Mock.Of<ICountryManagementService>(),
            Mock.Of<IBlockedAttemptRepository>(),
            Mock.Of<ILogger<IpBlockingService>>());

        var result = await svc.LookupIpAddressAsync("198.51.100.10");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LookupIpAddressAsync_returns_success_with_data()
    {
        var expected = new IpLookupResponse
        {
            IpAddress = "198.51.100.10",
            CountryCode = "US",
            CountryName = "United States"
        };

        var geo = new Mock<IGeolocationService>();
        geo.Setup(g => g.LookupIpAddressAsync(It.IsAny<string?>()))
           .ReturnsAsync(expected);

        var svc = new IpBlockingService(
            geo.Object,
            Mock.Of<ICountryManagementService>(),
            Mock.Of<IBlockedAttemptRepository>(),
            Mock.Of<ILogger<IpBlockingService>>());

        var result = await svc.LookupIpAddressAsync("198.51.100.10");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("US", true)]
    [InlineData("US", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task CheckIpBlockStatusAsync_sets_flags_and_logs_attempt(string? countryCode, bool isBlocked)
    {
        var geoResponse = new IpLookupResponse
        {
            IpAddress = "203.0.113.9",
            CountryCode = countryCode ?? "Unknown",
            CountryName = "Test"
        };

        var geo = new Mock<IGeolocationService>();
        geo.Setup(g => g.LookupIpAddressAsync(It.IsAny<string>()))
           .ReturnsAsync(geoResponse);

        var countries = new Mock<ICountryManagementService>();
        countries.Setup(c => c.IsCountryBlockedAsync(It.IsAny<string>()))
                 .ReturnsAsync(isBlocked);

        var repo = new Mock<IBlockedAttemptRepository>();
        repo.Setup(r => r.AddAttemptAsync(It.IsAny<BlockedAttempt>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var logger = Mock.Of<ILogger<IpBlockingService>>();

        var svc = new IpBlockingService(geo.Object, countries.Object, repo.Object, logger);
        var http = CreateHttpContext("TestAgent/1.0");

        var result = await svc.CheckIpBlockStatusAsync("203.0.113.9", http);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IpAddress.Should().Be("203.0.113.9");
        result.Data!.CountryCode.Should().Be(geoResponse.CountryCode);
        result.Data!.IsBlocked.Should().Be(isBlocked && !string.IsNullOrEmpty(countryCode) && countryCode != "Unknown");

        repo.Verify(r => r.AddAttemptAsync(It.Is<BlockedAttempt>(a =>
            a.IpAddress == "203.0.113.9" &&
            a.CountryCode == geoResponse.CountryCode &&
            a.IsBlocked == result.Data.IsBlocked &&
            a.UserAgent == "TestAgent/1.0"
        )), Times.Once);
    }
}


