using System.Text.Json;

namespace Catalyst.Core;

public static class AtomicJsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static T LoadOrDefault<T>(string path, Func<T> createDefault)
    {
        if (!File.Exists(path))
        {
            return createDefault();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options) ?? createDefault();
        }
        catch (Exception exception)
        {
            string corruptPath = $"{path}.corrupt.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            try
            {
                File.Copy(path, corruptPath, overwrite: false);
            }
            catch
            {
                // Preserve the original error and fall back to defaults.
            }

            CatalystLog.Warning("Save", $"Failed to load '{path}': {exception.Message}");
            return createDefault();
        }
    }

    public static void Save<T>(string path, T value)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = $"{path}.tmp";
        string json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, path, overwrite: true);
    }
}
