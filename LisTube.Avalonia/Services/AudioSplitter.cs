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

        segments.Sort((a, b) => a.Start.CompareTo(b.Start));

        return segments;
    }

    private static bool TryParseTimestamp(string s, out TimeSpan result)
    {
        result = TimeSpan.Zero;

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
        Action<double, string>? onProgress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException("Arquivo de áudio não encontrado para split.", sourceFile);

        if (segments.Count == 0)
            return;

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

            ct.Register(() =>
            {
                try { process.Kill(); } catch { }
            });

            process.Start();

            var errorOutput = new StringWriter();
            var stderrTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync(ct).ConfigureAwait(false)) != null)
                    {
                        errorOutput.WriteLine(line);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
            }, ct);

            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            await stderrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var stderr = errorOutput.ToString();
                var summary = ExtractFfmpegError(stderr);
                throw new InvalidOperationException(
                    $"ffmpeg falhou ao dividir '{segment.Name}' (código {process.ExitCode}): {summary}");
            }

            onProgress?.Invoke((double)(i + 1) / totalSegments, $"{FormatTimestamp(segment.Start)} - {segment.Name}");
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

        var lastLines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lastLines.Length > 0
            ? lastLines[^1].Trim()
            : "erro desconhecido";
    }
}
