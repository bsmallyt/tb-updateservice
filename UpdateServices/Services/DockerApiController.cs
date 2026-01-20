using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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

  private async Task<string> RestartContainerByService(string service)
  {
    try
    {
      // grab the container corresponding to service name
      var container = await GetContainerByServiceAsync(service);
      if (container == null)
        return $"Service {service} not found.";

      // get container info 
      var imageName = container.Image;
      var containerInfo = await _client.Containers.InspectContainerAsync(container.ID);

      // grab specific container configs
      var envVars = containerInfo.Config.Env;
      var restartPolicy = containerInfo.HostConfig.RestartPolicy;
      var mounts = containerInfo.Mounts.Select(x => new HostConfigMount
      {
        Source = x.Source,
        Target = x.Destination,
        Type = x.Type
      }).ToList();
      var networkNames = containerInfo.NetworkSettings.Networks.Keys.ToList();

      //stop container
      await StopServiceByContainer(container);

      //remove container
      await RemoveServiceByContainer(container);

      //pull latest image
      await PullImageByContainer(container);

      var newContainer = await _client.Containers.CreateContainerAsync(
        new CreateContainerParameters
        {
          Name = service,
          Image = imageName,
          Env = envVars,
          HostConfig = new HostConfig
          {
            RestartPolicy = restartPolicy,
            Mounts = mounts,
            PortBindings = container.Ports.ToDictionary(
              p => p.PrivatePort.ToString(),
              p => (IList<PortBinding>) new List<PortBinding> 
              { 
                new PortBinding { HostPort = p.PublicPort.ToString() }
              }
            )
          }
        }
      );

      //attatch networks
      foreach (var networkName in networkNames)
      {
        await _client.Networks.ConnectNetworkAsync(networkName, new NetworkConnectParameters
        {
          Container = newContainer.ID
        });
      }

      //start container
      await StartServiceByContainer(newContainer);

      return $"Service {service} successfully rescreated and started.";
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

  private async Task<string> StartServiceByContainer(ContainerListResponse container)
  {
    try
    {
      var result = await _client.Containers.StartContainerAsync(
        container.ID, 
        new ContainerStartParameters
        {

        },
        CancellationToken.None
      );

      if (result)
        return $"Container {container.ID} successfully started.";
      else 
        return $"Container {container.ID} was already started or could not be started.";
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

  private async Task<string> StopServiceByContainer(ContainerListResponse container)
  {
    try
    {
      var result = await _client.Containers.StopContainerAsync(
        container.ID,
        new ContainerStopParameters
        {
          WaitBeforeKillSeconds = 10
        },
        CancellationToken.None
      );

      if (result)
        return $"Container {container.ID} successfully stopped.";
      else
        return $"Container {container.ID} was already stopped or could not be stopped.";
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

  private async Task<string> RemoveServiceByContainer(ContainerListResponse container)
  {
    try
    {
      var result = await _client.Containers.RemoveContainerAsync(
        container.ID,
        new ContainerRemoveParameters
        {
          Force = true
        },
        CancellationToken.None
      );

      if (result) 
        return $"Container {container} successfully removed.";
      else
        throw new Exception($"Container {container} was already removed or could not be removed.");
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

  private async Task<string> PullImageByContainer(ContainerListResponse container)
  {
    try
    {
      var result = await _client.Images.CreateImageAsync(
        new ImagesCreateParameters
        {
          FromImage = container.Image.Split(':')[0],
          Tag = container.Image.Contains(":") ? container.Image.Split(':')[1] : "latest"
        },
        null,
        new Progress<JSONMessage>(message => {})
      );

      if (result)
        return $"Image {container.Image} successfully pulled.";
      else 
        throw new Exception($"Unable to pull image {container.Image}");
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