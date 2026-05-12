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
using LisTube.Avalonia.Models;
using LisTube.Avalonia.Services;

namespace LisTube.Avalonia.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
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

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

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
    private bool _isSingleVideo;

    [ObservableProperty]
    private bool _isSplitEnabled;

    [ObservableProperty]
    private string _splitText = string.Empty;

    public bool IsSplitVisible =>
        _isSingleVideo && !SelectedFormat.Contains("Vídeo");

    partial void OnSelectedFormatChanged(string value)
    {
        OnPropertyChanged(nameof(IsSplitVisible));
    }

    private static string ExtractCleanVideoId(string url)
    {
        // Handle HTML-encoded &amp; in URLs copied from web pages
        url = url.Replace("&amp;", "&");

        // Handle youtu.be URLs
        if (url.Contains("youtu.be"))
        {
            var uri = new Uri(url);
            var id = uri.AbsolutePath.TrimStart('/');
            var qIndex = id.IndexOf('?');
            return qIndex > 0 ? id[..qIndex] : id;
        }

        // Handle youtube.com URLs - extract v parameter properly
        if (url.Contains("youtube.com/watch") || url.Contains("/live/") || url.Contains("/shorts/"))
        {
            var uri = new Uri(url);
            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0] == "v")
                    return kv[1];
            }
            // If no v param in query, fall through
        }

        // For /shorts/ URLs, extract from path
        if (url.Contains("/shorts/"))
        {
            var uri = new Uri(url);
            var id = uri.AbsolutePath.TrimStart('/').Replace("shorts/", "");
            var qIndex = id.IndexOf('?');
            return qIndex > 0 ? id[..qIndex] : id;
        }

        return url;
    }

    [RelayCommand]
    private async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(YoutubeUrl))
            return;

        try
        {
            HasError = false;
            ErrorMessage = string.Empty;
            StatusMessage = "Carregando...";
            IsPlaylistLoaded = false;
            _isSingleVideo = false;
            IsSplitEnabled = false;
            SplitText = string.Empty;

            var url = YoutubeUrl.Trim();

            if (url.Contains("playlist?list="))
            {
                var playlistId = PlaylistId.Parse(url);
                _currentPlaylist = await YoutubeClientFactory.Current.Playlists.GetAsync(playlistId);

                PlaylistTitle = _currentPlaylist.Title;
                PlaylistAuthor = $"Autor: {_currentPlaylist.Author}";

                var videos = await YoutubeClientFactory.Current.Playlists.GetVideosAsync(playlistId).CollectAsync();
                VideoCount = $"Total de vídeos: {videos.Count}";

                Videos.Clear();
                _isSingleVideo = false;
                OnPropertyChanged(nameof(IsSplitVisible));
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
            else if (url.Contains("youtube.com/watch") || url.Contains("youtu.be")
                     || url.Contains("/live/") || url.Contains("/shorts/"))
            {
                var cleanUrl = ExtractCleanVideoId(url);
                var videoId = VideoId.Parse(cleanUrl);

                try
                {
                    var video = await YoutubeClientFactory.Current.Videos.GetAsync(videoId);

                    PlaylistTitle = video.Title;
                    PlaylistAuthor = $"Autor: {video.Author.ToString()}";
                    VideoCount = "Total de vídeos: 1";

                    Videos.Clear();
                    _isSingleVideo = true;
                    OnPropertyChanged(nameof(IsSplitVisible));
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
                catch (YoutubeExplode.Exceptions.VideoUnplayableException ex)
                {
                    // Video requires auth but yt-dlp may still download it
                    // Show what we can with the video ID
                    PlaylistTitle = $"Vídeo: {videoId.Value}";
                    PlaylistAuthor = "(requer autenticação)";
                    VideoCount = "Total de vídeos: 1";

                    Videos.Clear();
                    _isSingleVideo = true;
                    OnPropertyChanged(nameof(IsSplitVisible));
                    Videos.Add(new VideoItem
                    {
                        Id = videoId,
                        Title = $"Vídeo protegido - {videoId.Value}",
                        Author = "YouTube",
                        Duration = "?",
                        IsSelected = true
                    });
                    StatusMessage = $"Aviso: {ex.Message} — tente baixar mesmo assim (yt-dlp pode funcionar)";
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = "URL não suportada. Use links de vídeo (watch, youtu.be, live, shorts) ou playlist do YouTube.";
                return;
            }

            IsPlaylistLoaded = true;
            if (string.IsNullOrEmpty(StatusMessage))
                StatusMessage = "";
        }
        catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
        {
            HasError = true;
            ErrorMessage = $"Vídeo indisponível: {ex.Message}";
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Erro: {ex.Message}";
            StatusMessage = "";
        }
    }

    [RelayCommand]
    private async Task AddToQueueAsync()
    {
        var selectedVideos = Videos.Where(v => v.IsSelected).ToList();
        if (selectedVideos.Count == 0) return;

        // Buscando o QueueViewModel através da injeção de dependência ou referência
        // Para simplificar nesse cenário, vamos usar uma comunicação direta via MainViewModel ou Singleton se necessário.
        // Como o MainViewModel detém as instâncias, vamos expor um evento ou usar um padrão de mensagens.
        
        // Pelo tempo e complexidade, vamos disparar um evento que o MainViewModel ou o próprio Gerenciador Global capture.
        var task = new PlaylistDownloadTask
        {
            Title = PlaylistTitle,
            Status = "Na Fila...",
            ProgressText = $"0/{selectedVideos.Count}"
        };

        // Vamos precisar de um lugar central para as Tasks ativas.
        // Adicionando ao singleton ou serviço global.
        DownloadQueueManager.Instance.AddTask(task, selectedVideos, SelectedFormat);
        
        StatusMessage = "Playlist adicionada à fila!";
        await Task.Delay(2000);
        StatusMessage = string.Empty;
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
            int failed = 0;
            int total = selectedVideos.Count;
            double currentVideoProgress = 0;

            foreach (var videoItem in selectedVideos)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                StatusMessage = $"Downloading: {videoItem.Title}";
                ProgressText = $"({completed + 1}/{total})";
                currentVideoProgress = 0;

                try
                {
                    var safeTitle = string.Join("_", videoItem.Title.Split(Path.GetInvalidFileNameChars()));
                    var videoUrl = $"https://www.youtube.com/watch?v={videoItem.Id}";

                    var filePath = SelectedFormat.Contains("Vídeo")
                        ? Path.Combine(saveDirectory, $"{safeTitle}.mp4")
                        : SelectedFormat.Contains("M4A")
                            ? Path.Combine(saveDirectory, $"{safeTitle}.m4a")
                            : Path.Combine(saveDirectory, $"{safeTitle}.mp3");

                    var videoProgress = new Progress<double>(p =>
                    {
                        currentVideoProgress = p;
                        ProgressPercent = ((completed + p) / total) * 100;
                    });

                    await YtDlpService.DownloadAsync(videoUrl, filePath, SelectedFormat, videoProgress, _cancellationTokenSource.Token);

                    if (IsSplitEnabled && !string.IsNullOrWhiteSpace(SplitText) && !SelectedFormat.Contains("Vídeo"))
                    {
                        StatusMessage = $"Dividindo: {videoItem.Title}";
                        var segments = AudioSplitter.ParseSplitText(SplitText);

                        if (segments.Count > 0)
                        {
                            var splitProgress = new Progress<double>(p =>
                            {
                                ProgressPercent = ((completed + 0.9 + p * 0.09) / total) * 100;
                            });

                            await AudioSplitter.SplitAsync(filePath, segments, saveDirectory, splitProgress, _cancellationTokenSource.Token);

                            try { File.Delete(filePath); } catch { }

                            StatusMessage = $"Dividido em {segments.Count} faixas: {videoItem.Title}";
                        }
                        else
                        {
                            StatusMessage = $"Aviso: texto de split inválido — arquivo completo mantido: {videoItem.Title}";
                        }
                    }

                    completed++;
                    ProgressPercent = (completed / (double)total) * 100;
                }
                catch (Exception ex)
                {
                    failed++;
                    StatusMessage = $"Falha: {videoItem.Title} - {ex.Message}";
                    await Task.Delay(3000);
                }
            }

            if (failed > 0)
                StatusMessage = $"Download concluído com {failed} falha(s). {completed}/{total} sucesso(s).";
            else
                StatusMessage = "Download completo!";
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
