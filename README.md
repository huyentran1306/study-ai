# Log Analyzer (Large Log Optimized)

Portable .NET tool that scans large log folders, extracts `ERROR` lines, groups by module, and generates a static HTML dashboard.

## Features

- Streaming reads with `FileStream` + `StreamReader` (no full-file memory load)
- Incremental scanning via `Storage/offsets.json`
- Date-based filename filtering (`yyyy-MM-dd` token)
- Parallel file processing with `Parallel.ForEach`
- Static output (`report/index.html` + `report/data.json`)
- Single-executable friendly dashboard template fallback (works even if `Templates/` is missing)
- Convenience root `index.html` redirect to the generated report (helps preview environments)

## Usage

```bash
dotnet run -- <log-folder> [yyyy-MM-dd] [report-folder]
```

Example:

```bash
dotnet run -- ./sample-logs 2026-03-14 report
```

Open:
- `report/index.html` (main dashboard)
- or root `index.html` (auto-redirect)

## Publish single executable

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output executable is under `bin/Release/net8.0/win-x64/publish/`.
