using System.Text.Json;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class SettingsStore(string? directory = null)
{
    private readonly string directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhoneStore.Admin");
    public ServerSettings Load()
    {
        try
        {
            var path = Path.Combine(directory, "settings.json");
            return File.Exists(path) ? JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(path)) ?? Default : Default;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) { return Default; }
    }
    public void Save(ServerSettings settings)
    {
        ServerAddress.Validate(settings.BaseUrl);
        Directory.CreateDirectory(directory);
        // This allowlisted record has only server URL and optional expected public readiness values.
        var path = Path.Combine(directory, "settings.json"); var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings)); File.Move(temporary, path, true);
    }
    private static ServerSettings Default => new("http://localhost:5080");
}
