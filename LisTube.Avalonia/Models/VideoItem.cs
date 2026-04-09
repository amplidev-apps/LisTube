using CommunityToolkit.Mvvm.ComponentModel;

namespace LisTube.Avalonia.Models;

public partial class VideoItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _duration = string.Empty;

    [ObservableProperty]
    private string _thumbnailUrl = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private bool _isDownloaded;

    [ObservableProperty]
    private string _downloadPath = string.Empty;
}
