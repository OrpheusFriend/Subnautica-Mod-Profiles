namespace SubnauticaProfileManager.Models;

public class Profile
{
    public string Name { get; set; } = string.Empty;
    public List<string> EnabledMods { get; set; } = new();
}
