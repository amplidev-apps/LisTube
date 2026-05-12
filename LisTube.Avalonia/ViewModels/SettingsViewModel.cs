using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LisTube.Avalonia.Services;

namespace LisTube.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _audioSavePath = AppSettings.AudioSavePath;
    
    [ObservableProperty]
    private string _videoSavePath = AppSettings.VideoSavePath;

    [ObservableProperty]
    private string _cookiesText = AppSettings.YouTubeCookies;

    [ObservableProperty]
    private string _cookieStatus = string.Empty;

    partial void OnAudioSavePathChanged(string value) => AppSettings.AudioSavePath = value;
    partial void OnVideoSavePathChanged(string value) => AppSettings.VideoSavePath = value;

    partial void OnCookiesTextChanged(string value)
    {
        AppSettings.YouTubeCookies = value;
        YoutubeClientFactory.Reset();
    }

    [RelayCommand]
    private void ClearCookies()
    {
        CookiesText = string.Empty;
        CookieStatus = "Cookies removidos.";
    }

    [RelayCommand]
    private void ShowInstructions()
    {
        CookieStatus =
            "1. Instale 'Get cookies.txt' (Chrome) ou 'cookies.txt' (Firefox) • " +
            "2. Acesse youtube.com e faça login • " +
            "3. Exporte os cookies (formato Netscape) • " +
            "4. Cole TODO o conteúdo no campo acima";
    }
}
