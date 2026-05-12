using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LisTube.Avalonia.Models;
using LisTube.Avalonia.Services;

namespace LisTube.Avalonia;

public class DownloadQueueManager
{
    private static DownloadQueueManager? _instance;
    public static DownloadQueueManager Instance => _instance ??= new DownloadQueueManager();

    public ObservableCollection<PlaylistDownloadTask> QueueItems { get; } = new();
    
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
                    var videoUrl = $"https://www.youtube.com/watch?v={videoItem.Id}";

                    var filePath = format.Contains("Vídeo")
                        ? Path.Combine(saveDirectory, $"{safeTitle}.mp4")
                        : format.Contains("M4A")
                            ? Path.Combine(saveDirectory, $"{safeTitle}.m4a")
                            : Path.Combine(saveDirectory, $"{safeTitle}.mp3");

                    var videoProgress = new Progress<double>(p =>
                    {
                        task.Progress = ((completed + p) / total) * 100;
                    });

                    await YtDlpService.DownloadAsync(videoUrl, filePath, format, videoProgress);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DownloadQueueManager] Erro no vídeo '{videoItem.Title}': {ex.Message}");
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
