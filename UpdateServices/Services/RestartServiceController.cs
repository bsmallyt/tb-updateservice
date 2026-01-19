using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UpdateServices.Services
{
  public class RestartServiceController
  {
    public RestartServiceController () {}

    public async Task<string> RestartServiceAysnc(string service)
    {
      var psi = new ProcessStartInfo
      {
        FileName = "docker",
        Arguments = $"compose up -d --pull always --force-recreate {service}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = new Process { StartInfo = psi };

      process.Start();

      string output = await process.StandardOutput.ReadToEndAsync();
      string error = await process.StandardError.ReadToEndAsync();

      await process.WaitForExitAsync();

      if (process.ExitCode != 0)
        throw new Exception($"Error restarting service: {error}");

      return output;
    }
  }
}