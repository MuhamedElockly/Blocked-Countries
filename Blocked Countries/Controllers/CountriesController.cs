using BlockedCountries.Business.Models.Requests;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlockedCountries.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController : ControllerBase
{
    private readonly ICountryManagementService _countryManagementService;

    public CountriesController(ICountryManagementService countryManagementService)
    {
        _countryManagementService = countryManagementService;
    }

   
    [HttpPost("block")]
    [ProducesResponseType(typeof(BlockedCountryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BlockCountry([FromBody] BlockCountryRequest request)
    {
        var result = await _countryManagementService.BlockCountryAsync(request);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 400, result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    
    [HttpDelete("block/{countryCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockCountry(string countryCode)
    {
        var result = await _countryManagementService.UnblockCountryAsync(countryCode);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 404, result.ErrorMessage);
        }

        return NoContent();
    }

    
    [HttpGet("blocked")]
    [ProducesResponseType(typeof(PagedResponse<BlockedCountryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockedCountries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _countryManagementService.GetBlockedCountriesAsync(page, pageSize, search);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 400, result.ErrorMessage);
        }

        return Ok(result.Data);
    }

   
    [HttpPost("temporal-block")]
    [ProducesResponseType(typeof(BlockedCountryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TemporalBlock([FromBody] TemporalBlockRequest request)
    {
        var result = await _countryManagementService.AddTemporalBlockAsync(request);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 400, result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}
