using CommunityToolkit.Mvvm.ComponentModel;

namespace LisTube.Avalonia.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "About LisTube";
}
