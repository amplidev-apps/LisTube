using CommunityToolkit.Mvvm.ComponentModel;

namespace LisTube.Avalonia.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Help & Documentation";
}
