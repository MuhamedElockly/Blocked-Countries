using BlockedCountries.Business.Models;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlockedCountries.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IpController : ControllerBase
{
    private readonly IIpBlockingService _ipBlockingService;

    public IpController(IIpBlockingService ipBlockingService)
    {
        _ipBlockingService = ipBlockingService;
    }

    /// <summary>
    /// Lookup country details for an IP address
    /// </summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IpLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupIp([FromQuery] string? ipAddress = null)
    {
        var result = await _ipBlockingService.LookupIpAddressAsync(ipAddress, HttpContext);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 404, result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Check if the caller's IP address (or its country) is blocked
    /// </summary>
    [HttpGet("check-block")]
    [ProducesResponseType(typeof(CheckBlockResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckBlock()
    {
        var result = await _ipBlockingService.CheckIpBlockStatusAsync(HttpContext);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 400, result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}
