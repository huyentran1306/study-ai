using System.Collections.Concurrent;
using System.Globalization;
using LogAnalyzer.Models;

namespace LogAnalyzer.Services;

public class LogScanner
{
    private readonly OffsetManager _offsetManager;

    public LogScanner(OffsetManager offsetManager)
    {
        _offsetManager = offsetManager;
    }

    public List<LogEntry> Scan(string rootFolder, DateOnly? filterDate)
    {
        var files = Directory.EnumerateFiles(rootFolder, "*.log", SearchOption.AllDirectories)
            .Where(f => filterDate is null || MatchesDate(f, filterDate.Value))
            .ToList();

        var results = new ConcurrentBag<LogEntry>();

        Parallel.ForEach(files, file =>
        {
            ProcessFile(file, results);
        });

        return results
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    private void ProcessFile(string file, ConcurrentBag<LogEntry> results)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var startOffset = _offsetManager.GetOffset(file);
        if (startOffset < 0 || startOffset > fs.Length)
        {
            startOffset = 0;
        }

        fs.Seek(startOffset, SeekOrigin.Begin);
        using var reader = new StreamReader(fs);

        if (startOffset > 0)
        {
            _ = reader.ReadLine();
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var entry = ParseLine(line, file);
            if (entry is not null)
            {
                results.Add(entry);
            }
        }

        _offsetManager.UpdateOffset(file, fs.Position);
    }

    private static LogEntry? ParseLine(string line, string file)
    {
        // Supported format example:
        // [2026-03-14 10:05:44] [payment] ERROR some message
        // Fallback: infer module from folder/file and keep raw message.
        DateTime timestamp = DateTime.MinValue;
        var module = InferModule(file);

        if (line.Length >= 21 && line[0] == '[')
        {
            var timestampRaw = line.Substring(1, 19);
            if (DateTime.TryParseExact(timestampRaw, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                timestamp = parsed;
            }
        }

        var moduleStart = line.IndexOf('[', 22);
        if (moduleStart >= 0)
        {
            var moduleEnd = line.IndexOf(']', moduleStart + 1);
            if (moduleEnd > moduleStart)
            {
                module = line[(moduleStart + 1)..moduleEnd];
            }
        }

        return new LogEntry
        {
            Timestamp = timestamp,
            Module = string.IsNullOrWhiteSpace(module) ? "unknown" : module,
            Level = "ERROR",
            Message = line,
            FileName = Path.GetFileName(file)
        };
    }

    private static bool MatchesDate(string path, DateOnly date)
    {
        var token = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fileName = Path.GetFileName(path);
        return fileName.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static string InferModule(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        return Path.GetFileName(directory);
    }
}
