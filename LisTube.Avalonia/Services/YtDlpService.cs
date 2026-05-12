using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LisTube.Avalonia.Services;

public static class YtDlpService
{
    private const string YtDlpPath = "yt-dlp";

    public static bool IsAvailable
    {
        get
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = YtDlpPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (proc == null) return false;
                proc.WaitForExit(3000);
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    private static readonly Regex ProgressRegex = new(
        @"\[download\]\s+(\d+\.?\d*)%",
        RegexOptions.Compiled);

    public static async Task DownloadAsync(
        string videoUrl,
        string outputFilePath,
        string format,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var args = BuildArgsList(videoUrl, outputFilePath, format);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorOutput = new StringWriter();

        process.EnableRaisingEvents = true;

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                errorOutput.WriteLine(e.Data);
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null && progress != null)
            {
                var match = ProgressRegex.Match(e.Data);
                if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                {
                    try { progress.Report(pct / 100.0); } catch { }
                }
            }
        };

        process.Exited += (_, _) =>
        {
            if (process.ExitCode == 0)
                tcs.TrySetResult();
            else
            {
                var stderr = errorOutput.ToString();
                tcs.TrySetException(new InvalidOperationException(
                    $"yt-dlp falhou (código {process.ExitCode}): {ExtractError(stderr)}"));
            }
        };

        cancellationToken.Register(() =>
        {
            try { process.Kill(); } catch { }
            tcs.TrySetCanceled();
        });

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        await tcs.Task;
    }

    private static string ExtractError(string output)
    {
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
                return trimmed;
        }
        return output.Length > 200 ? output[..200] + "..." : output;
    }

    private static List<string> BuildArgsList(string videoUrl, string outputFilePath, string format)
    {
        var args = new List<string>();

        // Auth
        if (BuildAuthArg() is { } authArgs)
            args.AddRange(authArgs);

        // Format selection
        if (format.Contains("Vídeo"))
        {
            args.Add("-f");
            args.Add("bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best");
            args.Add("--merge-output-format");
            args.Add("mp4");
        }
        else if (format.Contains("320kbps"))
        {
            args.Add("-f");
            args.Add("bestaudio");
            args.Add("--extract-audio");
            args.Add("--audio-format");
            args.Add("mp3");
            args.Add("--audio-quality");
            args.Add("0");
        }
        else if (format.Contains("Padrão"))
        {
            args.Add("-f");
            args.Add("bestaudio");
            args.Add("--extract-audio");
            args.Add("--audio-format");
            args.Add("mp3");
            args.Add("--audio-quality");
            args.Add("5");
        }
        else
        {
            var ext = Path.GetExtension(outputFilePath).TrimStart('.').ToLowerInvariant();
            args.Add("-f");
            args.Add($"bestaudio[ext={ext}]/bestaudio");
        }

        // JS runtime (required for YouTube signature solving)
        if (IsNodeAvailable())
        {
            args.Add("--js-runtimes");
            args.Add("node");
            args.Add("--remote-components");
            args.Add("ejs:github");
        }

        // Common args
        args.Add("--no-playlist");
        args.Add("--newline");
        args.Add("-o");
        args.Add(outputFilePath);
        args.Add(videoUrl);

        return args;
    }

    private static string[]? BuildAuthArg()
    {
        var cookies = AppSettings.YouTubeCookies;
        if (!string.IsNullOrWhiteSpace(cookies))
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"listube_cookies_{Environment.ProcessId}.txt");
            try
            {
                File.WriteAllText(tempFile, cookies);
                return ["--cookies", tempFile];
            }
            catch { }
        }

        var browsers = new[] { "chrome", "brave", "firefox", "chromium", "edge" };
        foreach (var browser in browsers)
        {
            if (IsBrowserInstalled(browser))
                return ["--cookies-from-browser", browser];
        }

        return null;
    }

    private static bool IsNodeAvailable()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) return false;
            proc.WaitForExit(2000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsBrowserInstalled(string browser)
    {
        var configDir = browser switch
        {
            "chrome" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "google-chrome"),
            "brave" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "BraveSoftware", "Brave-Browser"),
            "firefox" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mozilla", "firefox"),
            "chromium" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "chromium"),
            "edge" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "microsoft-edge"),
            _ => null
        };

        return configDir != null && Directory.Exists(configDir);
    }
}
