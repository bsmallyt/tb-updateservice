using System.ComponentModel.DataAnnotations;
using Docker.DotNet;
using Docker.DotNet.Models; 

namespace UpdateServices.Services;

public class DockerApiController
{
  private readonly DockerClient _client;
  public DockerApiController()
  {
    _client = new DockerClientConfiguration(
      new Uri("unix:///var/run/docker.sock"))
      .CreateClient();
  }

  private async Task<ContainerListResponse?> GetContainerByServiceAsync(string service)
  {
    var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
    {
      All = true
    });

    return containers.FirstOrDefault(c =>
      c.Labels.TryGetValue("com.docker.compose.service", out var val) && val == service
    );
  }

  public async Task<string> StopService(string service)
  {
    try
    {
      var container = await GetContainerByServiceAsync(service);
      if (container == null)
        return $"No container found for service {service}";

      var result = await _client.Containers.StopContainerAsync(
        container.ID,
        new ContainerStopParameters
        {
          WaitBeforeKillSeconds = 10
        },
        CancellationToken.None
      );

      if (result)
        return $"Container {service} successfully stopped.";
      else
        return $"Container {service} was already stopped or could not be stopped.";
    } 
    catch (DockerApiException ex)
    {
      throw new Exception($"Docker Api Exception: {ex.Message}");
    }
    catch (Exception ex)
    {
      throw new Exception($"Unexpected error: {ex.Message}");
    }
  }
}