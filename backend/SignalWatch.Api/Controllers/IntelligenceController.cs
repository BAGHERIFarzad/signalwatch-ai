using Microsoft.AspNetCore.Mvc;
using SignalWatch.Api.Models;
using SignalWatch.Api.Services;

namespace SignalWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IntelligenceController : ControllerBase
{
    private readonly SignalOrchestratorService _signalOrchestratorService;

    public IntelligenceController(SignalOrchestratorService signalOrchestratorService)
    {
        _signalOrchestratorService = signalOrchestratorService;
    }

    [HttpPost("demo")]
    public async Task<ActionResult<IntelligenceReport>> GenerateDemoReport(
        [FromBody] IntelligenceRequest request)
    {
        var report = await _signalOrchestratorService.GenerateDemoReportAsync(request);
        return Ok(report);
    }

    [HttpPost("live")]
    public async Task<ActionResult<IntelligenceReport>> GenerateLiveReport(
        [FromBody] IntelligenceRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _signalOrchestratorService.GenerateLiveReportAsync(
            request,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            status = "SignalWatch AI backend is running",
            timestamp = DateTime.UtcNow
        });
    }
}