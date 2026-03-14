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

## Usage (on machine that has dotnet)

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

---

## Run on VM **without dotnet**

Yes, you can run it without installing dotnet on VM.

### 1) Build single self-contained EXE on another machine (Dev machine)

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output path:

```text
bin/Release/net8.0/win-x64/publish/LogAnalyzer.exe
```

### 2) Copy EXE to VM

Copy only `LogAnalyzer.exe` to the VM (for example: `D:\tools\LogAnalyzer.exe`).

### 3) Run on VM

From CMD/PowerShell in the folder containing the EXE:

```powershell
.\LogAnalyzer.exe .\sample-logs 2026-03-14
```

Or with custom report folder:

```powershell
.\LogAnalyzer.exe .\sample-logs 2026-03-14 .\report
```

### 4) Open report

- Open `report\index.html`
- or open root `index.html` (it redirects to `report/index.html`)

---

## Example for file-share path

```powershell
.\LogAnalyzer.exe "\\fileshare\logs" 2026-03-14 "D:\reports\log-analyzer"
```

## Notes

- If you run daily, offsets are saved under `Storage\offsets.json` near the EXE to avoid re-reading old bytes.
- If a log file is rotated/truncated, the scanner safely resets that file offset to 0.
