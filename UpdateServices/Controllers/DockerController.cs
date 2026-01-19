using System.Net;
using Microsoft.AspNetCore.Mvc;
using UpdateServices.Services;

namespace UpdateServices.Controllers;

[ApiController]
[Route("docker")]
public class DockerController : ControllerBase
{
  private readonly RestartServiceController _restartService;
  private readonly DockerApiController _dockerService;
  public DockerController(RestartServiceController restartService, DockerApiController dockerService)
  {
    _restartService = restartService;
    _dockerService = dockerService;
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

  [HttpPost("stop/{service}")]
  public async Task<IActionResult> StopService(string service)
  {
    try
    {
      var output = await _dockerService.StopService(service);
      return Ok(new { message = "Service stopped", output });
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = ex.Message });
    }
  }
}