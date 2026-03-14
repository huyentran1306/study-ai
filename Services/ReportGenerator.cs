using System.Text.Json;
using LogAnalyzer.Models;

namespace LogAnalyzer.Services;

public class ReportGenerator
{
    private readonly string? _templatePath;

    public ReportGenerator(string? templatePath = null)
    {
        _templatePath = templatePath;
    }

    public void Generate(string reportFolder, Dictionary<string, int> moduleCounts, List<LogEntry> entries, DateOnly? filterDate)
    {
        Directory.CreateDirectory(reportFolder);

        var reportData = new
        {
            generatedAtUtc = DateTime.UtcNow,
            filterDate = filterDate?.ToString("yyyy-MM-dd"),
            totalErrors = entries.Count,
            modules = moduleCounts,
            sampleErrors = entries.Take(200).Select(e => new
            {
                timestamp = e.Timestamp == DateTime.MinValue ? null : e.Timestamp,
                module = e.Module,
                file = e.FileName,
                message = e.Message
            })
        };

        var json = JsonSerializer.Serialize(reportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var dataPath = Path.Combine(reportFolder, "data.json");
        File.WriteAllText(dataPath, json);

        // Single-executable friendly: if template file is unavailable, use embedded fallback HTML.
        var templateHtml = ResolveTemplate();
        var indexPath = Path.Combine(reportFolder, "index.html");
        File.WriteAllText(indexPath, templateHtml);

        // Convenience file for environments that auto-open only root /index.html.
        var rootIndexPath = Path.Combine(Directory.GetCurrentDirectory(), "index.html");
        var relativeTarget = Path.GetRelativePath(Directory.GetCurrentDirectory(), indexPath)
            .Replace("\\", "/");

        File.WriteAllText(rootIndexPath,
            $"<!doctype html><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"0; url={relativeTarget}\"><title>Redirecting...</title><a href=\"{relativeTarget}\">Open report</a>");
    }

    private string ResolveTemplate()
    {
        if (!string.IsNullOrWhiteSpace(_templatePath) && File.Exists(_templatePath))
        {
            return File.ReadAllText(_templatePath);
        }

        return DefaultTemplate;
    }

    private const string DefaultTemplate = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Log Analyzer Dashboard</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 24px; color: #1f2937; }
    h1 { margin-bottom: 4px; }
    .meta { color: #4b5563; margin-bottom: 20px; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
    .card { border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; }
    .bar { background: #2563eb; color: white; padding: 6px 8px; border-radius: 4px; margin: 6px 0; white-space: nowrap; overflow: hidden; }
    table { width: 100%; border-collapse: collapse; }
    th, td { border-bottom: 1px solid #e5e7eb; text-align: left; padding: 8px; font-size: 14px; }
    th { background: #f9fafb; }
    .mono { font-family: Consolas, Monaco, monospace; }
    @media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <h1>Log Analyzer Dashboard</h1>
  <div class="meta" id="meta">Loading...</div>

  <div class="grid">
    <section class="card">
      <h2>Error Count by Module</h2>
      <div id="bars"></div>
    </section>

    <section class="card">
      <h2>Top Modules</h2>
      <table>
        <thead><tr><th>Module</th><th>Errors</th></tr></thead>
        <tbody id="topModules"></tbody>
      </table>
    </section>
  </div>

  <section class="card" style="margin-top:24px;">
    <h2>Sample Error Lines</h2>
    <table>
      <thead><tr><th>Timestamp</th><th>Module</th><th>File</th><th>Message</th></tr></thead>
      <tbody id="errorRows"></tbody>
    </table>
  </section>

  <script>
    fetch('data.json')
      .then(r => r.json())
      .then(data => {
        const modules = Object.entries(data.modules || {});
        const maxValue = Math.max(1, ...modules.map(([,v]) => v));

        const meta = document.getElementById('meta');
        meta.textContent = `Generated UTC: ${data.generatedAtUtc} | Date filter: ${data.filterDate || 'none'} | Total errors: ${data.totalErrors}`;

        const bars = document.getElementById('bars');
        modules.forEach(([name, count]) => {
          const row = document.createElement('div');
          const pct = Math.max(3, Math.round((count / maxValue) * 100));
          row.className = 'bar';
          row.style.width = pct + '%';
          row.textContent = `${name}: ${count}`;
          bars.appendChild(row);
        });

        const topBody = document.getElementById('topModules');
        modules.slice(0, 20).forEach(([name, count]) => {
          const tr = document.createElement('tr');
          tr.innerHTML = `<td>${escapeHtml(name)}</td><td>${count}</td>`;
          topBody.appendChild(tr);
        });

        const rowBody = document.getElementById('errorRows');
        (data.sampleErrors || []).forEach(item => {
          const tr = document.createElement('tr');
          tr.innerHTML = `<td>${item.timestamp || ''}</td><td>${escapeHtml(item.module || '')}</td><td>${escapeHtml(item.file || '')}</td><td class="mono">${escapeHtml(item.message || '')}</td>`;
          rowBody.appendChild(tr);
        });
      })
      .catch(err => {
        document.getElementById('meta').textContent = `Failed to load data.json: ${err}`;
      });

    function escapeHtml(text) {
      const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
      return String(text).replace(/[&<>"']/g, m => map[m]);
    }
  </script>
</body>
</html>
""";
}
