using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using LisTube.Avalonia.Models;

namespace LisTube.Avalonia.ViewModels;

public partial class QueueViewModel : ViewModelBase
{
    public ObservableCollection<PlaylistDownloadTask> Items => DownloadQueueManager.Instance.QueueItems;
}
