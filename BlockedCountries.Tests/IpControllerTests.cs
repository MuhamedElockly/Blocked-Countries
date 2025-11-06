using System.Threading.Tasks;
using BlockedCountries.Api.Controllers;
using BlockedCountries.Business;
using BlockedCountries.Business.Models;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BlockedCountries.Tests;

public class IpControllerTests
{
    private static DefaultHttpContext CreateHttp(string? remoteIp = null, string? userAgent = null)
    {
        var ctx = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(remoteIp))
        {
            ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        }
        if (!string.IsNullOrEmpty(userAgent))
        {
            ctx.Request.Headers["User-Agent"] = userAgent;
        }
        return ctx;
    }

    [Fact]
    public async Task LookupIp_returns_ok_with_data_when_success()
    {
        var ipService = new Mock<IIpBlockingService>();
        var response = new IpLookupResponse { IpAddress = "198.51.100.5", CountryCode = "US", CountryName = "United States" };
        ipService.Setup(s => s.LookupIpAddressAsync("198.51.100.5"))
                 .ReturnsAsync(ServiceResult<IpLookupResponse>.Success(response));

        var controller = new IpController(ipService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttp() }
        };

        var result = await controller.LookupIp("198.51.100.5");

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task LookupIp_uses_remote_when_ip_not_provided()
    {
        var ipService = new Mock<IIpBlockingService>();
        var response = new IpLookupResponse { IpAddress = "203.0.113.12", CountryCode = "GB", CountryName = "United Kingdom" };
        ipService.Setup(s => s.LookupIpAddressAsync("203.0.113.12"))
                 .ReturnsAsync(ServiceResult<IpLookupResponse>.Success(response));

        var controller = new IpController(ipService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttp(remoteIp: "203.0.113.12") }
        };

        var result = await controller.LookupIp(null);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        (ok.Value as IpLookupResponse)!.IpAddress.Should().Be("203.0.113.12");
    }

    [Fact]
    public async Task LookupIp_propagates_error_status()
    {
        var ipService = new Mock<IIpBlockingService>();
        ipService.Setup(s => s.LookupIpAddressAsync("198.51.100.7"))
                 .ReturnsAsync(ServiceResult<IpLookupResponse>.Failure("not found", 404));

        var controller = new IpController(ipService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttp() }
        };

        var result = await controller.LookupIp("198.51.100.7");

        var obj = result as ObjectResult;
        obj.Should().NotBeNull();
        obj!.StatusCode.Should().Be(404);
        obj.Value.Should().Be("not found");
    }

    [Fact]
    public async Task CheckBlock_returns_ok_with_expected_payload()
    {
        var ipService = new Mock<IIpBlockingService>();
        var resp = new CheckBlockResponse { IpAddress = "192.0.2.10", CountryCode = "CA", IsBlocked = true };
        ipService.Setup(s => s.CheckIpBlockStatusAsync("192.0.2.10", It.IsAny<HttpContext>()))
                 .ReturnsAsync(ServiceResult<CheckBlockResponse>.Success(resp));

        var controller = new IpController(ipService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttp(remoteIp: "192.0.2.10", userAgent: "UA/1.0") }
        };

        var result = await controller.CheckBlock();
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(resp);
    }
}


