using CommunityToolkit.Mvvm.ComponentModel;

namespace LisTube.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _audioSavePath = AppSettings.AudioSavePath;
    
    [ObservableProperty]
    private string _videoSavePath = AppSettings.VideoSavePath;

    partial void OnAudioSavePathChanged(string value) => AppSettings.AudioSavePath = value;
    partial void OnVideoSavePathChanged(string value) => AppSettings.VideoSavePath = value;
}
