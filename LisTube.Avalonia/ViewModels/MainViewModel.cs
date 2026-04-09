using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LisTube.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private MainPageViewModel _mainPageViewModel;

    private readonly QueueViewModel _queueViewModel = new QueueViewModel();
    private readonly SettingsViewModel _settingsViewModel = new SettingsViewModel();
    private readonly AboutViewModel _aboutViewModel = new AboutViewModel();
    private readonly HelpViewModel _helpViewModel = new HelpViewModel();

    public MainViewModel()
    {
        _mainPageViewModel = new MainPageViewModel();
        _currentView = _mainPageViewModel;
    }

    [RelayCommand]
    private void NavigateHome()
    {
        CurrentView = MainPageViewModel;
    }

    [RelayCommand]
    private void NavigateQueue()
    {
        CurrentView = _queueViewModel;
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        CurrentView = _settingsViewModel;
    }

    [RelayCommand]
    private void NavigateAbout()
    {
        CurrentView = _aboutViewModel;
    }

    [RelayCommand]
    private void NavigateHelp()
    {
        CurrentView = _helpViewModel;
    }
}
