
using BlockedCountries.Business.Models;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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

   
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IpLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupIp([FromQuery] string? ipAddress = null)
    {
        if (ipAddress == null) {
            ipAddress = GetClientIpAddress(HttpContext);
        }
        var result = await _ipBlockingService.LookupIpAddressAsync(ipAddress);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 404, result.ErrorMessage);
        }

        return Ok(result.Data);
    }
    private string GetClientIpAddress(HttpContext context)
    {

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                return remoteIp.MapToIPv4().ToString();
            }
            return remoteIp.ToString();
        }

        return "127.0.0.1";
    }


    [HttpGet("check-block")]
    [ProducesResponseType(typeof(CheckBlockResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckBlock()
    {
      string  ipAddress = GetClientIpAddress(HttpContext);
        
        var result = await _ipBlockingService.CheckIpBlockStatusAsync(ipAddress,HttpContext);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 400, result.ErrorMessage);
        }

        return Ok(result.Data);
    }
   
}
