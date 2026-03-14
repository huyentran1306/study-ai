using LogAnalyzer.Services;

if (args.Length == 0)
{
    Console.WriteLine("Usage: LogAnalyzer <log-folder> [yyyy-MM-dd] [report-folder]");
    Console.WriteLine("Example: LogAnalyzer \\\\fileshare\\logs 2026-03-14 report");
    return;
}

var logFolder = args[0];
if (!Directory.Exists(logFolder))
{
    Console.Error.WriteLine($"Log folder does not exist: {logFolder}");
    return;
}

DateOnly? filterDate = null;
if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
{
    if (DateOnly.TryParse(args[1], out var parsedDate))
    {
        filterDate = parsedDate;
    }
    else
    {
        Console.Error.WriteLine($"Invalid date format '{args[1]}'. Use yyyy-MM-dd.");
        return;
    }
}

var reportFolder = args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2])
    ? args[2]
    : "report";

var appFolder = AppContext.BaseDirectory;
var storageFolder = Path.Combine(appFolder, "Storage");
Directory.CreateDirectory(storageFolder);
var offsetsPath = Path.Combine(storageFolder, "offsets.json");

var offsetManager = new OffsetManager(offsetsPath);
var scanner = new LogScanner(offsetManager);

var entries = scanner.Scan(logFolder, filterDate);
var summary = AggregationService.CountByModule(entries);

var generator = new ReportGenerator(Path.Combine(appFolder, "Templates", "index.html"));
generator.Generate(reportFolder, summary, entries, filterDate);

offsetManager.Save();

Console.WriteLine($"Scanned entries: {entries.Count}");
Console.WriteLine($"Modules found: {summary.Count}");
Console.WriteLine($"Report generated: {Path.GetFullPath(Path.Combine(reportFolder, "index.html"))}");
