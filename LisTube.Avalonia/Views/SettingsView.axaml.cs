using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LisTube.Avalonia.ViewModels;

namespace LisTube.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void BrowseAudio_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Selecione a pasta para Áudios" });
            if (folders.Count > 0 && DataContext is SettingsViewModel vm)
            {
                vm.AudioSavePath = folders[0].Path.LocalPath;
            }
        }
    }

    private async void BrowseVideo_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Selecione a pasta para Vídeos" });
            if (folders.Count > 0 && DataContext is SettingsViewModel vm)
            {
                vm.VideoSavePath = folders[0].Path.LocalPath;
            }
        }
    }
}
