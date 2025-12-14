using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace M18BatteryInfo
{
    public static class PythonScriptRunner
    {
        public static string RunPythonScript(string arguments)
        {
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "m18.py");

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" {arguments}".Trim(),
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();

            process.Start();

            outputBuilder.Append(process.StandardOutput.ReadToEnd());
            outputBuilder.Append(process.StandardError.ReadToEnd());

            process.WaitForExit();

            return outputBuilder.ToString();
        }
    }
}
