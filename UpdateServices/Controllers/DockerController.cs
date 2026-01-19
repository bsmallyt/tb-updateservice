using Microsoft.AspNetCore.Mvc;
using UpdateServices.Services;

namespace UpdateServices.Controllers;

[ApiController]
[Route("docker")]
public class DockerController : ControllerBase
{
  private readonly RestartServiceController _restartService;
  public DockerController(RestartServiceController restartService)
  {
    _restartService = restartService;
  }

  [HttpPost("restart/{service}")]
  public async Task<IActionResult> RestartService(string service)
  {
    try
    {
      var output = await _restartService.RestartServiceAsync(service);
      return Ok(new { message = "Service restarted", output });
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = ex.Message });
    }
  }
}