using System;
using System.Collections.Generic;
using System.IO;
using SubnauticaProfileManager.Models;

namespace SubnauticaProfileManager.Services;

public class ModScanner
{
    public List<ModInfo> ScanMods(string gamePath)
    {
        var mods = new List<ModInfo>();
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            return mods;
        }

        var pluginsPath = Path.Combine(gamePath, "BepInEx", "plugins");
        var disabledPath = Path.Combine(gamePath, "BepInEx", "plugins_disabled");

        AddModsFromDirectory(mods, pluginsPath, true);
        AddModsFromDirectory(mods, disabledPath, false);

        return mods;
    }

    private static void AddModsFromDirectory(List<ModInfo> mods, string directoryPath, bool enabled)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(directoryPath))
        {
            var name = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            mods.Add(new ModInfo
            {
                Name = name,
                IsEnabled = enabled
            });
        }
    }
}
