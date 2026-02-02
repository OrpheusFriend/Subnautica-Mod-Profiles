using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using SubnauticaProfileManager.Models;
using SubnauticaProfileManager.Services;

namespace SubnauticaProfileManager.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GameLocator _gameLocator = new();
    private readonly ModScanner _modScanner = new();
    private readonly ProfileManager _profileManager = new();
    private readonly Launcher _launcher = new();
    private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

    private string _gamePath = string.Empty;
    private Profile? _selectedProfile;
    private string _statusMessage = string.Empty;

    public MainViewModel()
    {
        Profiles = new ObservableCollection<Profile>();
        Mods = new ObservableCollection<ModInfo>();

        DetectGamePathCommand = new RelayCommand(_ => DetectGamePath());
        BrowseGamePathCommand = new RelayCommand(_ => BrowseForGamePath());
        RefreshModsCommand = new RelayCommand(_ => RefreshMods());
        AddProfileCommand = new RelayCommand(_ => AddProfile());
        DeleteProfileCommand = new RelayCommand(_ => DeleteProfile(), _ => SelectedProfile != null);
        SaveProfileCommand = new RelayCommand(_ => SaveProfile(), _ => SelectedProfile != null);
        SwitchProfileCommand = new RelayCommand(_ => SwitchProfile(), _ => SelectedProfile != null);
        LaunchGameCommand = new RelayCommand(_ => LaunchGame());

        LoadConfig();
        LoadProfiles();
        RefreshMods();
    }

    public ObservableCollection<Profile> Profiles { get; }
    public ObservableCollection<ModInfo> Mods { get; }

    public string GamePath
    {
        get => _gamePath;
        set
        {
            if (SetField(ref _gamePath, value))
            {
                SaveConfig();
                RefreshMods();
            }
        }
    }

    public Profile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetField(ref _selectedProfile, value))
            {
                UpdateModsForProfile();
                RaiseCommandState();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand DetectGamePathCommand { get; }
    public ICommand BrowseGamePathCommand { get; }
    public ICommand RefreshModsCommand { get; }
    public ICommand AddProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand SwitchProfileCommand { get; }
    public ICommand LaunchGameCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            GamePath = _gameLocator.DetectDefaultPath();
            return;
        }

        var json = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        GamePath = string.IsNullOrWhiteSpace(config.GamePath) ? _gameLocator.DetectDefaultPath() : config.GamePath;
    }

    private void SaveConfig()
    {
        var config = new AppConfig
        {
            GamePath = GamePath
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profileManager.LoadProfiles())
        {
            Profiles.Add(profile);
        }

        if (Profiles.Any())
        {
            SelectedProfile = Profiles.First();
        }
    }

    private void RefreshMods()
    {
        Mods.Clear();
        foreach (var mod in _modScanner.ScanMods(GamePath))
        {
            Mods.Add(mod);
        }

        UpdateModsForProfile();
    }

    private void UpdateModsForProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        var enabled = new HashSet<string>(SelectedProfile.EnabledMods, StringComparer.OrdinalIgnoreCase);
        foreach (var mod in Mods)
        {
            mod.IsEnabled = enabled.Contains(mod.Name);
        }

        OnPropertyChanged(nameof(Mods));
    }

    private void AddProfile()
    {
        var name = Microsoft.VisualBasic.Interaction.InputBox("Enter a new profile name:", "New Profile", "New Profile");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (Profiles.Any(profile => string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Profile name already exists.";
            return;
        }

        var profile = new Profile
        {
            Name = name,
            EnabledMods = Mods.Where(mod => mod.IsEnabled).Select(mod => mod.Name).ToList()
        };

        _profileManager.SaveProfile(profile);
        Profiles.Add(profile);
        SelectedProfile = profile;
        StatusMessage = "Profile created.";
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        var name = SelectedProfile.Name;
        _profileManager.DeleteProfile(name);
        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.FirstOrDefault();
        StatusMessage = $"Deleted profile {name}.";
    }

    private void SaveProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        SelectedProfile.EnabledMods = Mods.Where(mod => mod.IsEnabled).Select(mod => mod.Name).ToList();
        _profileManager.SaveProfile(SelectedProfile);
        StatusMessage = "Profile saved.";
    }

    private void SwitchProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            SelectedProfile.EnabledMods = Mods.Where(mod => mod.IsEnabled).Select(mod => mod.Name).ToList();
            _profileManager.SaveProfile(SelectedProfile);
            _profileManager.SwitchProfile(GamePath, SelectedProfile);
            RefreshMods();
            StatusMessage = "Profile switched.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private void LaunchGame()
    {
        try
        {
            _launcher.Launch(GamePath);
            StatusMessage = "Launching Subnautica...";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private void DetectGamePath()
    {
        GamePath = _gameLocator.DetectDefaultPath();
        StatusMessage = _gameLocator.IsValidGamePath(GamePath)
            ? "Detected game path."
            : "Default path not found. Please browse manually.";
    }

    private void BrowseForGamePath()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select your Subnautica installation folder"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            GamePath = dialog.SelectedPath;
            StatusMessage = _gameLocator.IsValidGamePath(GamePath)
                ? "Game path updated."
                : "Selected folder does not contain Subnautica.exe.";
        }
    }

    private void RaiseCommandState()
    {
        if (DeleteProfileCommand is RelayCommand deleteCommand)
        {
            deleteCommand.RaiseCanExecuteChanged();
        }

        if (SaveProfileCommand is RelayCommand saveCommand)
        {
            saveCommand.RaiseCanExecuteChanged();
        }

        if (SwitchProfileCommand is RelayCommand switchCommand)
        {
            switchCommand.RaiseCanExecuteChanged();
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
