# Audio Split Feature

Split a downloaded YouTube video into MP3 tracks by timestamp, for album-in-one-video cases.

## Scope

- Individual video only (not playlists)
- Audio formats only (MP3 320kbps, MP3 Padrão, M4A/AAC)
- Post-processing: download full audio via yt-dlp, then split with ffmpeg

## UI

### Main page additions (MainView.axaml)

After loading a single video (not playlist), when user selects an audio format:

- **Checkbox** "Dividir em faixas" — bound to `IsSplitEnabled` on MainPageViewModel
- **Text area** (multiline, visible when IsSplitEnabled is true)
  - Placeholder: `0:00 - Intro\n3:30 - Música 1\n7:45 - Música 2`
  - Bound to `SplitText`
- The Download button flow is unchanged

### Visibility rules

- Split controls hidden when a playlist is loaded
- Split controls hidden when "Vídeo (Alta Qualidade MP4)" format is selected
- When user switches format back to audio, controls reappear

## Split Text Format

```
MM:SS - Track Name
H:MM:SS - Track Name
```

- One track per line
- Separator: ` - ` (space, hyphen, space) — exactly this separator
- Timestamp format: `M:SS`, `MM:SS`, or `H:MM:SS`
- Track Name is everything after ` - ` on the line
- Empty lines are ignored
- Lines that don't match the pattern are ignored (with warning logged)

### Parsing rules

1. Split text by newline
2. For each non-empty line, split on ` - `
3. Parse first part as `[H:]MM:SS` → `TimeSpan`
4. Rest is track name (trimmed)

## Data Flow

### New model: `SplitSegment`

```csharp
public record SplitSegment(TimeSpan Start, string Name);
```

### Download + Split flow (DownloadAsync in MainPageViewModel)

1. User clicks Download with audio format + SplitText filled
2. Full MP3 downloaded via `YtDlpService.DownloadAsync` (existing flow, high quality)
3. If `IsSplitEnabled && !string.IsNullOrWhiteSpace(SplitText)`:
   a. Parse `SplitText` into `List<SplitSegment>`
   b. Call `AudioSplitter.SplitAsync(fullPath, segments, progress)`
   c. ffmpeg splits and outputs `{TrackName}.mp3` for each segment
   d. Delete full MP3
   e. Status message: "Dividido em {N} faixas!"
4. If no split text: existing behavior (save as single file)

### Progress during split

Progress bar during split phase uses `IProgress<double>`:
- 0-70%: yt-dlp download (existing)
- 70-95%: ffmpeg splits (each segment contributes equal portion)
- 95-100%: cleanup

## New Service: `AudioSplitter`

```csharp
public static class AudioSplitter
{
    public static async Task SplitAsync(
        string sourceFile,
        IReadOnlyList<SplitSegment> segments,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
```

### ffmpeg invocation

For each segment (except last):
```
ffmpeg -i "{source}" -ss {start} -to {nextStart} -c copy -y "{outputDir}/{name}.mp3"
```

For last segment:
```
ffmpeg -i "{source}" -ss {start} -c copy -y "{outputDir}/{name}.mp3"
```

Uses `ProcessStartInfo.ArgumentList` with `ffmpeg` (same pattern as YtDlpService).

### Rationale for `-c copy`

- No re-encode: fast, no quality loss
- MP3 frame size is ~26ms — cut accuracy is sufficient for track splitting
- Avoids needing libmp3lame or other encoder dependencies

## Edge Cases

| Case | Behavior |
|------|----------|
| Empty split text | Download as single file (existing behavior) |
| Only one segment in split text | Download + single split (effectively a rename) |
| Timestamps out of order | Sort ascending before splitting |
| Overlapping timestamps | Treat as sequential (sorted order, no dedup) |
| Invalid timestamp line | Skip line, continue with valid ones |
| ffmpeg not installed | Show clear error: "ffmpeg não encontrado. Instale com: sudo apt install ffmpeg" |
| ffmpeg fails mid-split | Report which track failed, partial output preserved |
| Special chars in track name | Sanitize filename (same as existing safeTitle logic) |
| Very long track name | Truncate to 200 chars |

## Files Changed

### New files
- `LisTube.Avalonia/Models/SplitSegment.cs` — record with TimeSpan + Name
- `LisTube.Avalonia/Services/AudioSplitter.cs` — ffmpeg wrapper for splitting

### Modified files
- `LisTube.Avalonia/ViewModels/MainPageViewModel.cs` — add IsSplitEnabled, SplitText, split logic in DownloadAsync
- `LisTube.Avalonia/Views/MainView.axaml` — add checkbox + text area

## Dependencies

- ffmpeg must be installed on the system (checked at runtime before split)
- No new NuGet packages required
