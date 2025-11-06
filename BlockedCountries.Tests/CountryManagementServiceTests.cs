using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockedCountries.Business.Models.Requests;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using BlockedCountries.Data.Models;
using BlockedCountries.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlockedCountries.Tests;

public class CountryManagementServiceTests
{
    [Fact]
    public async Task BlockCountry_validates_and_adds_when_not_blocked()
    {
        var repo = new Mock<ICountryRepository>();
        repo.Setup(r => r.GetBlockedCountryAsync("US"))
            .ReturnsAsync((CountryInfo?)null);
        repo.Setup(r => r.AddBlockedCountryAsync(It.IsAny<CountryInfo>()))
            .ReturnsAsync(true);

        var svc = new CountryManagementService(repo.Object, Mock.Of<ILogger<CountryManagementService>>());

        var result = await svc.BlockCountryAsync(new BlockCountryRequest { CountryCode = "us" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.CountryCode.Should().Be("US");
        repo.Verify(r => r.AddBlockedCountryAsync(It.Is<CountryInfo>(c => c.CountryCode == "US")), Times.Once);
    }

    [Fact]
    public async Task BlockCountry_returns_existing_when_already_blocked()
    {
        var existing = new CountryInfo { CountryCode = "GB", CountryName = "United Kingdom" };
        var repo = new Mock<ICountryRepository>();
        repo.Setup(r => r.GetBlockedCountryAsync("GB")).ReturnsAsync(existing);

        var svc = new CountryManagementService(repo.Object, Mock.Of<ILogger<CountryManagementService>>());

        var result = await svc.BlockCountryAsync(new BlockCountryRequest { CountryCode = "GB" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.CountryCode.Should().Be("GB");
        repo.Verify(r => r.AddBlockedCountryAsync(It.IsAny<CountryInfo>()), Times.Never);
    }

    [Fact]
    public async Task UnblockCountry_returns_success_when_removed()
    {
        var repo = new Mock<ICountryRepository>();
        repo.Setup(r => r.RemoveBlockedCountryAsync("CA"))
            .ReturnsAsync(true);

        var svc = new CountryManagementService(repo.Object, Mock.Of<ILogger<CountryManagementService>>());
        var result = await svc.UnblockCountryAsync("ca");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task UnblockCountry_returns_failure_when_not_found()
    {
        var repo = new Mock<ICountryRepository>();
        repo.Setup(r => r.RemoveBlockedCountryAsync("DE"))
            .ReturnsAsync(false);

        var svc = new CountryManagementService(repo.Object, Mock.Of<ILogger<CountryManagementService>>());
        var result = await svc.UnblockCountryAsync("DE");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetBlockedCountries_applies_pagination_and_search_and_filters_temporal()
    {
        var data = new List<CountryInfo>
        {
            new() { CountryCode = "US", CountryName = "United States", BlockedAt = System.DateTime.UtcNow },
            new() { CountryCode = "CA", CountryName = "Canada", BlockedAt = System.DateTime.UtcNow },
            new() { CountryCode = "GB", CountryName = "United Kingdom", BlockedAt = System.DateTime.UtcNow, IsTemporalBlock = true, ExpiresAt = System.DateTime.UtcNow.AddMinutes(-5) },
            new() { CountryCode = "DE", CountryName = "Germany", BlockedAt = System.DateTime.UtcNow }
        };

        var repo = new Mock<ICountryRepository>();
        repo.Setup(r => r.GetAllBlockedCountriesAsync()).ReturnsAsync(data);

        var svc = new CountryManagementService(repo.Object, Mock.Of<ILogger<CountryManagementService>>());

        var result = await svc.GetBlockedCountriesAsync(page: 1, pageSize: 2, searchTerm: "a");

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(3); 
        result.Data!.Items.Count.Should().Be(2);
        result.Data!.Items.Select(i => i.CountryCode).Should().ContainInOrder(new[] { "US", "CA" });
    }
}


