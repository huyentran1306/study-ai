namespace LogAnalyzer.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
