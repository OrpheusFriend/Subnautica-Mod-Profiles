using System;
using System.Diagnostics;
using System.IO;

namespace SubnauticaProfileManager.Services;

public class Launcher
{
    public void Launch(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            throw new InvalidOperationException("Game path is not set.");
        }

        var exePath = Path.Combine(gamePath, "Subnautica.exe");
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("Subnautica.exe not found.", exePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = gamePath
        };

        Process.Start(startInfo);
    }
}
