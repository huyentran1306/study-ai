using System.Text.Json;

namespace LogAnalyzer.Services;

public class OffsetManager
{
    private readonly string _offsetFile;
    private readonly Dictionary<string, long> _offsets;
    private readonly object _sync = new();

    public OffsetManager(string offsetFile)
    {
        _offsetFile = offsetFile;
        _offsets = LoadOffsets(offsetFile);
    }

    public long GetOffset(string file)
    {
        lock (_sync)
        {
            return _offsets.TryGetValue(file, out var offset) ? offset : 0;
        }
    }

    public void UpdateOffset(string file, long position)
    {
        lock (_sync)
        {
            _offsets[file] = position;
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(_offsetFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Dictionary<string, long> snapshot;
        lock (_sync)
        {
            snapshot = new Dictionary<string, long>(_offsets);
        }

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_offsetFile, json);
    }

    private static Dictionary<string, long> LoadOffsets(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            return data is null
                ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, long>(data, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
