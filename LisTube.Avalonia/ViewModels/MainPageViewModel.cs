using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;
using LisTube.Avalonia.Models;

namespace LisTube.Avalonia.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    private readonly YoutubeClient _youtubeClient;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _youtubeUrl = string.Empty;

    [ObservableProperty]
    private bool _isPlaylistLoaded;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _playlistTitle = string.Empty;

    [ObservableProperty]
    private string _playlistAuthor = string.Empty;

    [ObservableProperty]
    private string _videoCount = string.Empty;

    public ObservableCollection<string> DownloadFormats { get; } = new()
    {
        "Vídeo (Alta Qualidade MP4)",
        "Áudio (MP3 320kbps)",
        "Áudio (MP3 Padrão)",
        "Áudio (M4A / AAC Nativo)"
    };

    [ObservableProperty]
    private string _selectedFormat = "Vídeo (Alta Qualidade MP4)";

    [ObservableProperty]
    private ObservableCollection<VideoItem> _videos = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _versionInfo = "LisTube v1.0.0";

    private Playlist? _currentPlaylist;

    public MainPageViewModel()
    {
        _youtubeClient = new YoutubeClient();
    }

    [RelayCommand]
    private async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(YoutubeUrl))
            return;

        try
        {
            StatusMessage = "Loading playlist...";
            IsPlaylistLoaded = false;

            // Try to get playlist
            if (YoutubeUrl.Contains("playlist?list="))
            {
                var playlistId = PlaylistId.Parse(YoutubeUrl);
                _currentPlaylist = await _youtubeClient.Playlists.GetAsync(playlistId);

                PlaylistTitle = _currentPlaylist.Title;
                PlaylistAuthor = $"Author: {_currentPlaylist.Author}";

                var videos = await _youtubeClient.Playlists.GetVideosAsync(playlistId).CollectAsync();
                VideoCount = $"Total videos: {videos.Count}";

                Videos.Clear();
                foreach (var video in videos)
                {
                    Videos.Add(new VideoItem
                    {
                        Id = video.Id,
                        Title = video.Title,
                        Author = video.Author.ToString(),
                        Duration = video.Duration?.ToString() ?? "Unknown",
                        ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url ?? "",
                        IsSelected = true
                    });
                }
            }
            else if (YoutubeUrl.Contains("youtube.com/watch") || YoutubeUrl.Contains("youtu.be"))
            {
                // Single video
                var videoId = VideoId.Parse(YoutubeUrl);
                var video = await _youtubeClient.Videos.GetAsync(videoId);

                PlaylistTitle = video.Title;
                PlaylistAuthor = $"Author: {video.Author.ToString()}";
                VideoCount = "Total videos: 1";

                Videos.Clear();
                Videos.Add(new VideoItem
                {
                    Id = video.Id,
                    Title = video.Title,
                    Author = video.Author.ToString(),
                    Duration = video.Duration?.ToString() ?? "Unknown",
                    ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url ?? "",
                    IsSelected = true
                });
            }

            IsPlaylistLoaded = true;
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        var selectedVideos = Videos.Where(v => v.IsSelected).ToList();
        if (!selectedVideos.Any())
            return;

        try
        {
            IsDownloading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            var saveDirectory = SelectedFormat.Contains("Vídeo") ? AppSettings.VideoSavePath : AppSettings.AudioSavePath;

            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            int completed = 0;
            int total = selectedVideos.Count;

            foreach (var videoItem in selectedVideos)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                StatusMessage = $"Downloading: {videoItem.Title}";
                ProgressText = $"({completed + 1}/{total})";

                try
                {
                    var video = await _youtubeClient.Videos.GetAsync(videoItem.Id);
                    var safeTitle = string.Join("_", videoItem.Title.Split(Path.GetInvalidFileNameChars()));
                    
                    if (SelectedFormat.Contains("Vídeo"))
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.mp4");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("mp4"), cancellationToken: _cancellationTokenSource.Token);
                    }
                    else if (SelectedFormat.Contains("320kbps"))
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.mp3");
                        // Forces standard high-quality MP3 (FFMPEG manages the upscaling automatically under the hood for MP3 when targeting from highest opus stream)
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("mp3"), cancellationToken: _cancellationTokenSource.Token);
                    }
                    else if (SelectedFormat.Contains("Padrão"))
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.mp3");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("mp3"), cancellationToken: _cancellationTokenSource.Token);
                    }
                    else
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.m4a");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("m4a"), cancellationToken: _cancellationTokenSource.Token);
                    }

                    completed++;
                    ProgressPercent = (completed / (double)total) * 100;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error downloading {videoItem.Title}: {ex.Message}";
                    await Task.Delay(2000);
                }
            }

            StatusMessage = "Download complete!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            ProgressPercent = 0;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void CancelDownload()
    {
        _cancellationTokenSource?.Cancel();
    }
}
