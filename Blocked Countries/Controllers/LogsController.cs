using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlockedCountries.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IBlockedAttemptService _attemptService;

    public LogsController(IBlockedAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    /// <summary>
    /// Get all blocked attempts with pagination
    /// </summary>
    [HttpGet("blocked-attempts")]
    [ProducesResponseType(typeof(PagedResponse<BlockedAttemptResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockedAttempts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _attemptService.GetBlockedAttemptsAsync(page, pageSize);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode ?? 400, result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}
