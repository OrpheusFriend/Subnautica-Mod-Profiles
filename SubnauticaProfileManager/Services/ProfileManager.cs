using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SubnauticaProfileManager.Models;

namespace SubnauticaProfileManager.Services;

public class ProfileManager
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public string ProfilesDirectory => Path.Combine(AppContext.BaseDirectory, "Profiles");

    public List<Profile> LoadProfiles()
    {
        EnsureProfilesDirectory();
        var profiles = new List<Profile>();
        foreach (var file in Directory.GetFiles(ProfilesDirectory, "*.json"))
        {
            var profile = LoadProfileFromFile(file);
            if (profile != null)
            {
                profiles.Add(profile);
            }
        }

        return profiles.OrderBy(profile => profile.Name).ToList();
    }

    public void SaveProfile(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new InvalidOperationException("Profile name cannot be empty.");
        }

        EnsureProfilesDirectory();
        var filePath = GetProfilePath(profile.Name);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public void DeleteProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return;
        }

        var filePath = GetProfilePath(profileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public Profile? LoadProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return null;
        }

        var filePath = GetProfilePath(profileName);
        return LoadProfileFromFile(filePath);
    }

    public void SwitchProfile(string gamePath, Profile profile)
    {
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            throw new InvalidOperationException("Game path is not set.");
        }

        var pluginsPath = Path.Combine(gamePath, "BepInEx", "plugins");
        var disabledPath = Path.Combine(gamePath, "BepInEx", "plugins_disabled");
        Directory.CreateDirectory(pluginsPath);
        Directory.CreateDirectory(disabledPath);

        var enabledMods = new HashSet<string>(profile.EnabledMods, StringComparer.OrdinalIgnoreCase);
        MoveMods(pluginsPath, disabledPath, enabledMods, true);
        MoveMods(disabledPath, pluginsPath, enabledMods, false);
    }

    private void MoveMods(string sourcePath, string targetPath, HashSet<string> enabledMods, bool currentlyEnabled)
    {
        if (!Directory.Exists(sourcePath))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            var name = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var shouldBeEnabled = enabledMods.Contains(name);
            if (shouldBeEnabled == currentlyEnabled)
            {
                continue;
            }

            var targetDir = Path.Combine(targetPath, name);
            if (Directory.Exists(targetDir))
            {
                continue;
            }

            Directory.Move(dir, targetDir);
        }
    }

    private string GetProfilePath(string profileName)
    {
        var sanitized = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(ProfilesDirectory, $"{sanitized}.json");
    }

    private Profile? LoadProfileFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);
        var profile = JsonSerializer.Deserialize<Profile>(json);
        if (profile == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            profile.Name = Path.GetFileNameWithoutExtension(filePath);
        }

        profile.EnabledMods ??= new List<string>();
        return profile;
    }

    private void EnsureProfilesDirectory()
    {
        Directory.CreateDirectory(ProfilesDirectory);
    }
}
