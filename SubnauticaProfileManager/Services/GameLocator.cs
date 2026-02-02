using System;
using System.IO;

namespace SubnauticaProfileManager.Services;

public class GameLocator
{
    public string DetectDefaultPath()
    {
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var steamPath = Path.Combine(programFilesX86, "Steam", "steamapps", "common", "Subnautica");
        return steamPath;
    }

    public bool IsValidGamePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var exePath = Path.Combine(path, "Subnautica.exe");
        return File.Exists(exePath);
    }
}
