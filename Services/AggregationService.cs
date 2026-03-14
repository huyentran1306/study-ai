using LogAnalyzer.Models;

namespace LogAnalyzer.Services;

public static class AggregationService
{
    public static Dictionary<string, int> CountByModule(IEnumerable<LogEntry> logs)
    {
        return logs
            .GroupBy(x => x.Module, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }
}
