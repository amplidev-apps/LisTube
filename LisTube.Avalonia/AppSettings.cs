using System;
using System.IO;

namespace LisTube.Avalonia;

public static class AppSettings
{
    public static string AudioSavePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "LisTube", "Audio");
    public static string VideoSavePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "LisTube", "Video");
}
