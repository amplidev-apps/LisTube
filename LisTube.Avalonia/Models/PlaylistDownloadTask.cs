using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace LisTube.Avalonia.Models;

public partial class PlaylistDownloadTask : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _status = "Aguardando..."; // Aguardando, Analisando, Baixando, Concluído, Erro

    [ObservableProperty]
    private double _progress = 0;

    [ObservableProperty]
    private string _progressText = "0/0";

    [ObservableProperty]
    private bool _isCompleted = false;

    [ObservableProperty]
    private bool _isError = false;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
}
