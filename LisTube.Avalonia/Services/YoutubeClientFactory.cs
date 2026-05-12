using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using YoutubeExplode;

namespace LisTube.Avalonia.Services;

public static class YoutubeClientFactory
{
    private static YoutubeClient? _current;
    private static string? _lastCookies;

    public static YoutubeClient Current
    {
        get
        {
            var currentCookies = AppSettings.YouTubeCookies ?? "";
            if (_current != null && _lastCookies == currentCookies)
                return _current;

            _current = BuildClient(currentCookies);
            _lastCookies = currentCookies;
            return _current;
        }
    }

    public static void Reset()
    {
        _current = null;
        _lastCookies = null;
    }

    private static YoutubeClient BuildClient(string cookiesText)
    {
        if (string.IsNullOrWhiteSpace(cookiesText))
            return new YoutubeClient();

        try
        {
            var cookies = ParseNetscapeCookies(cookiesText);
            if (cookies.Count > 0)
                return new YoutubeClient(cookies);
        }
        catch
        {
        }

        return new YoutubeClient();
    }

    private static List<Cookie> ParseNetscapeCookies(string cookiesText)
    {
        var cookies = new List<Cookie>();

        using var reader = new StringReader(cookiesText);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split('\t');
            if (parts.Length < 7)
                continue;

            var domain = parts[0];
            var path = parts[2];
            var secure = parts[3] == "TRUE";
            var name = parts[5];
            var value = parts[6];

            if (!domain.Contains(".youtube.com", StringComparison.OrdinalIgnoreCase)
                && !domain.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                continue;

            cookies.Add(new Cookie(name, value, path, domain)
            {
                Secure = secure,
                HttpOnly = false
            });
        }

        return cookies;
    }
}
