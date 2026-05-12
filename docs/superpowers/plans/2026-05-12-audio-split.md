# Audio Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow users to split a downloaded YouTube video into named MP3 tracks by timestamp, for album-in-one-video cases.

**Architecture:** ffmpeg post-processes the full MP3 downloaded by yt-dlp. A new `AudioSplitter` service parses the timestamp text and calls ffmpeg once per segment. The main view gets a checkbox + textarea for split input, hidden for playlists/video formats.

**Tech Stack:** .NET 8, Avalonia, ffmpeg (external), yt-dlp (external)

---

### Task 1: Create SplitSegment model

**Files:**
- Create: `LisTube.Avalonia/Models/SplitSegment.cs`

- [ ] **Step 1: Create record**

Write to `LisTube.Avalonia/Models/SplitSegment.cs`:

```csharp
namespace LisTube.Avalonia.Models;

public record SplitSegment(TimeSpan Start, string Name);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build LisTube.Avalonia/LisTube.Avalonia.csproj`
Expected: build succeeds

---

### Task 2: Create AudioSplitter service

**Files:**
- Create: `LisTube.Avalonia/Services/AudioSplitter.cs`

- [ ] **Step 1: Create AudioSplitter with ParseSplitText and SplitAsync**

Write to `LisTube.Avalonia/Services/AudioSplitter.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LisTube.Avalonia.Models;

namespace LisTube.Avalonia.Services;

public static class AudioSplitter
{
    private const string FfmpegPath = "ffmpeg";

    /// <summary>
    /// Parse split text in format:
    /// MM:SS - Name
    /// H:MM:SS - Name
    /// One segment per line.
    /// </summary>
    public static List<SplitSegment> ParseSplitText(string text)
    {
        var segments = new List<SplitSegment>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var dashIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
            if (dashIndex < 0)
                continue;

            var timestampPart = trimmed[..dashIndex].Trim();
            var namePart = trimmed[(dashIndex + 3)..].Trim();

            if (string.IsNullOrEmpty(timestampPart) || string.IsNullOrEmpty(namePart))
                continue;

            if (!TryParseTimestamp(timestampPart, out var start))
                continue;

            segments.Add(new SplitSegment(start, namePart));
        }

        // Sort by start time
        segments.Sort((a, b) => a.Start.CompareTo(b.Start));

        return segments;
    }

    private static bool TryParseTimestamp(string s, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        // Format: M:SS, MM:SS, or H:MM:SS
        var parts = s.Split(':');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out var minutes) &&
                int.TryParse(parts[1], out var seconds))
            {
                result = new TimeSpan(0, minutes, seconds);
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var minutes) &&
                int.TryParse(parts[2], out var seconds))
            {
                result = new TimeSpan(hours, minutes, seconds);
                return true;
            }
        }

        return false;
    }

    public static async Task SplitAsync(
        string sourceFile,
        IReadOnlyList<SplitSegment> segments,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException("Arquivo de áudio não encontrado para split.", sourceFile);

        if (segments.Count == 0)
            return;

        // Check ffmpeg availability
        try
        {
            using var checkProc = Process.Start(new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (checkProc == null)
                throw new InvalidOperationException("ffmpeg não encontrado. Instale com: sudo apt install ffmpeg");
            checkProc.WaitForExit(3000);
            if (checkProc.ExitCode != 0)
                throw new InvalidOperationException("ffmpeg não encontrado. Instale com: sudo apt install ffmpeg");
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception)
        {
            throw new InvalidOperationException("ffmpeg não encontrado. Instale com: sudo apt install ffmpeg");
        }

        var totalSegments = segments.Count;

        for (int i = 0; i < totalSegments; i++)
        {
            ct.ThrowIfCancellationRequested();

            var segment = segments[i];
            var safeName = string.Join("_", segment.Name.Split(Path.GetInvalidFileNameChars()));
            var outputPath = Path.Combine(outputDirectory, $"{safeName}.mp3");

            var args = new List<string>
            {
                "-i", sourceFile,
                "-ss", FormatTimestamp(segment.Start)
            };

            // Add -to for all segments except the last
            if (i < totalSegments - 1)
            {
                args.Add("-to");
                args.Add(FormatTimestamp(segments[i + 1].Start));
            }

            args.Add("-c");
            args.Add("copy");
            args.Add("-avoid_negative_ts");
            args.Add("make_zero");
            args.Add("-y");
            args.Add(outputPath);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
                process.StartInfo.ArgumentList.Add(arg);

            var errorOutput = new StringWriter();
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    errorOutput.WriteLine(e.Data);
            };

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            process.Exited += (_, _) =>
            {
                if (process.ExitCode == 0)
                    tcs.TrySetResult();
                else
                {
                    var stderr = errorOutput.ToString();
                    var summary = ExtractFfmpegError(stderr);
                    tcs.TrySetException(new InvalidOperationException(
                        $"ffmpeg falhou ao dividir '{segment.Name}' (código {process.ExitCode}): {summary}"));
                }
            };

            ct.Register(() =>
            {
                try { process.Kill(); } catch { }
                tcs.TrySetCanceled();
            });

            process.Start();
            process.BeginErrorReadLine();
            await tcs.Task;

            progress?.Report((double)(i + 1) / totalSegments);
        }
    }

    private static string FormatTimestamp(TimeSpan ts)
    {
        if (ts.Hours > 0)
            return $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private static string ExtractFfmpegError(string stderr)
    {
        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("built with", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("configuration", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("lib", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("Input", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("Duration", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("Stream", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("Output", StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.Contains("speed="))
                continue;
            if (trimmed.Contains("bitrate="))
                continue;
            if (trimmed.StartsWith("  ", StringComparison.Ordinal))
                continue;

            if (trimmed.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Invalid", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }
        }

        // Return the last few non-empty lines
        var lastLines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lastLines.Length > 0
            ? lastLines[^1].Trim()
            : "erro desconhecido";
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build LisTube.Avalonia/LisTube.Avalonia.csproj`
Expected: build succeeds

---

### Task 3: Update MainPageViewModel with split properties and logic

**Files:**
- Modify: `LisTube.Avalonia/ViewModels/MainPageViewModel.cs`

- [ ] **Step 1: Add split-related properties**

Add these fields after `_currentPlaylist` field (line 73):

```csharp
    private bool _isSingleVideo;

    [ObservableProperty]
    private bool _isSplitEnabled;

    [ObservableProperty]
    private string _splitText = string.Empty;
```

- [ ] **Step 2: Set _isSingleVideo in LoadPlaylistAsync**

In the single-video branch (after line 207, before `IsPlaylistLoaded = true;`), add:

```csharp
            _isSingleVideo = true;
```

In the playlist branch and catch block, ensure `_isSingleVideo = false;` is set where appropriate. Specifically:
- After playlist loads (line 153), add `_isSingleVideo = false;`
- Inside the single-video catch blocks and normal path, set `_isSingleVideo = true;` for single video
- Also reset `IsSplitEnabled = false;` and `SplitText = string.Empty;` when loading starts

Actually, replace lines 154-153 area to also clear split state. The safest approach: set `_isSingleVideo` and clear split state at the top of `LoadPlaylistAsync`. Edit lines 122-124:

```csharp
            HasError = false;
            ErrorMessage = string.Empty;
            StatusMessage = "Carregando...";
            IsPlaylistLoaded = false;
            _isSingleVideo = false;
            IsSplitEnabled = false;
            SplitText = string.Empty;
```

Then after each successful load where Videos has items:
- In the playlist branch (after `Videos.Clear()` on line 141): `_isSingleVideo = false;`
- In the single-video branch (after `Videos.Clear()` on line 169): `_isSingleVideo = true;`
- In the VideoUnplayableException branch (after `Videos.Clear()` on line 189): `_isSingleVideo = true;`

- [ ] **Step 3: Add CanSplit helper property**

Add after the split properties:

```csharp
    public bool IsSplitVisible =>
        _isSingleVideo && !SelectedFormat.Contains("Vídeo") && Videos.Count == 1;
```

Also add partial method so visibility updates when format changes:

```csharp
    partial void OnSelectedFormatChanged(string value)
    {
        OnPropertyChanged(nameof(IsSplitVisible));
    }
```

- [ ] **Step 4: Notify IsSplitVisible when videos change**

Add after `Videos.Clear()` calls, trigger a refresh. The simplest approach: extend `OnSelectedFormatChanged` or add after video loading. Actually, the `_isSingleVideo` field combined with `Videos.Count == 1` already covers this. But `IsSplitVisible` depends on `Videos.Count` which isn't observable. Let me simplify: just use `_isSingleVideo` and `SelectedFormat`:

Replace `IsSplitVisible` with:

```csharp
    public bool IsSplitVisible =>
        _isSingleVideo && !SelectedFormat.Contains("Vídeo");
```

And after setting `_isSingleVideo` in LoadPlaylistAsync, call:

```csharp
    OnPropertyChanged(nameof(IsSplitVisible));
```

- [ ] **Step 5: Add ffmpeg check and split logic in DownloadAsync**

In the `DownloadAsync` method, after a successful yt-dlp download (after line 302 where `await YtDlpService.DownloadAsync(...)` completes), add the split logic:

Replace the block from the `await YtDlpService.DownloadAsync(...)` call through the `completed++;` line (lines 302-304):

```csharp
                    await YtDlpService.DownloadAsync(videoUrl, filePath, SelectedFormat, videoProgress, _cancellationTokenSource.Token);

                    // Split if enabled and has split text
                    if (IsSplitEnabled && !string.IsNullOrWhiteSpace(SplitText) && !SelectedFormat.Contains("Vídeo"))
                    {
                        StatusMessage = $"Dividindo: {videoItem.Title}";
                        var segments = AudioSplitter.ParseSplitText(SplitText);

                        if (segments.Count > 0)
                        {
                            var splitProgress = new Progress<double>(p =>
                            {
                                // Download was 0-90%, split phase is 90-99%
                                ProgressPercent = ((completed + 0.9 + p * 0.09) / total) * 100;
                            });

                            await AudioSplitter.SplitAsync(filePath, segments, saveDirectory, splitProgress, _cancellationTokenSource.Token);

                            // Delete the full file after successful split
                            try { File.Delete(filePath); } catch { }

                            StatusMessage = $"Dividido em {segments.Count} faixas: {videoItem.Title}";
                        }
                        else
                        {
                            StatusMessage = $"Aviso: texto de split inválido — arquivo completo mantido: {videoItem.Title}";
                        }
                    }

                    completed++;
```

Also add `using LisTube.Avalonia.Services;` namespace usage at top of file (it's already there on line 15).

- [ ] **Step 6: Clear split state on URL change or new load**

Add at the start of `LoadPlaylistAsync`, right after the early return check (after line 122):

```csharp
            IsSplitEnabled = false;
            SplitText = string.Empty;
```

- [ ] **Step 7: Build to verify**

Run: `dotnet build LisTube.Avalonia/LisTube.Avalonia.csproj`
Expected: build succeeds

---

### Task 4: Update MainPageView.axaml with split UI

**Files:**
- Modify: `LisTube.Avalonia/Views/MainPageView.axaml`

- [ ] **Step 1: Add split controls after the playlist info section**

Insert after the download buttons section (after the `</Border>` at line 118 that closes the playlist info border) and before the Error Message border (line 121):

```xml
                <!-- Split Controls -->
                <Border Background="#3E3E42"
                        CornerRadius="8"
                        Padding="20"
                        IsVisible="{Binding IsSplitVisible}">
                    <StackPanel Spacing="10">
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <CheckBox IsChecked="{Binding IsSplitEnabled}"
                                      Foreground="#FFFFFF"
                                      VerticalAlignment="Center" />
                            <TextBlock Text="Dividir em faixas por timestamp"
                                       Foreground="#FFFFFF"
                                       FontSize="14"
                                       VerticalAlignment="Center" />
                        </StackPanel>
                        <TextBox Text="{Binding SplitText}"
                                 Watermark="0:00 - Intro&#x0a;3:30 - Música 1&#x0a;7:45 - Música 2"
                                 Height="100"
                                 FontSize="13"
                                 AcceptsReturn="True"
                                 IsVisible="{Binding IsSplitEnabled}"
                                 TextWrapping="NoWrap" />
                    </StackPanel>
                </Border>
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build LisTube.Avalonia/LisTube.Avalonia.csproj`
Expected: build succeeds

---

### Task 5: Final verification

- [ ] **Step 1: Build the project**

Run: `dotnet build LisTube.Avalonia/LisTube.Avalonia.csproj 2>&1`
Expected: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

- [ ] **Step 2: Test with actual yt-dlp + ffmpeg**

Create a test script to verify the full flow works:

```bash
# Download a short audio with yt-dlp
yt-dlp -f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 -o "/tmp/test_split_full.mp3" "https://www.youtube.com/watch?v=dQw4w9WgXcQ" 2>&1

# Test splitting with ffmpeg
ffmpeg -i "/tmp/test_split_full.mp3" -ss 0:00 -to 0:30 -c copy -avoid_negative_ts make_zero -y "/tmp/test_split_part1.mp3" 2>&1
ffmpeg -i "/tmp/test_split_full.mp3" -ss 0:30 -to 1:00 -c copy -avoid_negative_ts make_zero -y "/tmp/test_split_part2.mp3" 2>&1
ffmpeg -i "/tmp/test_split_full.mp3" -ss 1:00 -c copy -avoid_negative_ts make_zero -y "/tmp/test_split_part3.mp3" 2>&1

ls -la /tmp/test_split_*.mp3
```

Expected: yt-dlp downloads, ffmpeg splits into 3 files, each playable.
