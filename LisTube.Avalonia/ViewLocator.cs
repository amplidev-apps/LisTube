using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LisTube.Avalonia.ViewModels;

namespace LisTube.Avalonia;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        Console.WriteLine($"ViewLocator.Build called with data: {data?.GetType().FullName}");
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        Console.WriteLine($"Resolved View type: {type?.FullName ?? "null"}");

        if (type != null)
        {
            try
            {
                var control = (Control)Activator.CreateInstance(type)!;
                control.DataContext = data;
                return control;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception creating {type.FullName}: {ex.InnerException?.Message ?? ex.Message}");
                Console.WriteLine(ex.InnerException?.StackTrace ?? ex.StackTrace);
                return new TextBlock { Text = "Crash: " + ex.InnerException?.Message };
            }
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        var matched = data is ViewModelBase;
        Console.WriteLine($"ViewLocator.Match called for {data?.GetType().FullName}. Result: {matched}");
        return matched;
    }
}
