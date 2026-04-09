using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LisTube.Avalonia.Models;
using YoutubeExplode;
using YoutubeExplode.Converter;

namespace LisTube.Avalonia;

public class DownloadQueueManager
{
    private static DownloadQueueManager? _instance;
    public static DownloadQueueManager Instance => _instance ??= new DownloadQueueManager();

    public ObservableCollection<PlaylistDownloadTask> QueueItems { get; } = new();
    
    private readonly YoutubeClient _youtubeClient = new();
    private readonly SemaphoreSlim _semaphore = new(20); // Limite de 20 simultâneos pedido pelo usuário

    public void AddTask(PlaylistDownloadTask task, List<VideoItem> videos, string format)
    {
        QueueItems.Add(task);
        _ = RunDownloadTask(task, videos, format);
    }

    private async Task RunDownloadTask(PlaylistDownloadTask task, List<VideoItem> videos, string format)
    {
        await _semaphore.WaitAsync();
        try
        {
            task.Status = "Iniciando...";
            var saveDirectory = format.Contains("Vídeo") ? AppSettings.VideoSavePath : AppSettings.AudioSavePath;
            if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);

            int completed = 0;
            int total = videos.Count;

            foreach (var videoItem in videos)
            {
                task.Status = $"Baixando: {videoItem.Title}";
                try
                {
                    var safeTitle = string.Join("_", videoItem.Title.Split(Path.GetInvalidFileNameChars()));
                    
                    if (format.Contains("Vídeo"))
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.mp4");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("mp4"));
                    }
                    else if (format.Contains("320kbps"))
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.mp3");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("mp3").SetPreset(ConversionPreset.UltraFast));
                    }
                    else if (format.Contains("Padrão"))
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.mp3");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("mp3"));
                    }
                    else
                    {
                        var filePath = Path.Combine(saveDirectory, $"{safeTitle}.m4a");
                        await _youtubeClient.Videos.DownloadAsync(videoItem.Id, filePath, builder => builder.SetContainer("m4a"));
                    }
                }
                catch (Exception ex)
                {
                    // Log error for specific video but continue playlist
                }

                completed++;
                task.Progress = (double)completed / total * 100;
                task.ProgressText = $"{completed}/{total}";
            }

            task.Status = "Concluído!";
            task.IsCompleted = true;
            task.Progress = 100;
        }
        catch (Exception ex)
        {
            task.Status = "Erro!";
            task.IsError = true;
            task.ErrorMessage = ex.Message;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
