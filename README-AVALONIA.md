# LisTube Avalonia Edition

A cross-platform version of LisTube built with Avalonia UI, running natively on Linux, macOS, and Windows.

## Features

- ✅ **Native Linux Support** - Runs natively on Linux without Wine
- ✅ **Same Core Functionality** - Download YouTube playlists, channels, and single videos
- ✅ **Modern UI** - Clean interface with dark theme
- ✅ **Multiple Formats** - Support for various video and audio formats
- ✅ **Progress Tracking** - Real-time download progress
- ✅ **Selective Downloads** - Choose which videos to download

## Requirements

- .NET 8.0 SDK or later
- Linux, macOS, or Windows

## Quick Start

### Option 1: Use the Build Script

```bash
./build-avalonia.sh
```

This will automatically install .NET SDK if needed and build the application.

### Option 2: Manual Build

```bash
# Install .NET 8.0 SDK first:
# https://dotnet.microsoft.com/download/dotnet/8.0

cd LisTube.Avalonia
dotnet restore
dotnet build
dotnet run
```

### Option 3: Build Self-Contained Executable

```bash
cd LisTube.Avalonia
dotnet publish --configuration Release --self-contained --runtime linux-x64 -o ../publish
```

## Running the Application

After building:

```bash
./run-avalonia.sh
```

Or directly:

```bash
./publish-avalonia/LisTube
```

## Differences from WPF Version

| Feature | WPF Version | Avalonia Version |
|---------|------------|------------------|
| Platform | Windows only | Linux, macOS, Windows |
| UI Framework | WPF (Windows-only) | Avalonia UI (cross-platform) |
| Appearance | Native Windows | Modern, consistent across platforms |
| Auto-Update | Yes | No (manual update) |
| MahApps.Metro | Yes | No (custom styling) |

## Project Structure

```
LisTube.Avalonia/
├── Assets/              # Images, icons, resources
├── Models/              # Data models (VideoItem, etc.)
├── Services/            # Business logic, download services
├── Styles/              # Theme and styling
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML Views
├── App.axaml            # Application resources
├── App.axaml.cs         # Application class
└── Program.cs           # Entry point
```

## Development

### Adding a New View

1. Create the XAML file in `Views/`
2. Create the code-behind `.axaml.cs`
3. Create the ViewModel in `ViewModels/`
4. Register navigation in `MainViewModel.cs`

### Adding Resources

Add language resources to `Assets/Languages/English.axaml`

## Troubleshooting

### Missing libicu

If you get an error about `libicu`, install it:

```bash
# Ubuntu/Debian
sudo apt-get install libicu-dev

# Fedora
sudo dnf install libicu

# Arch
sudo pacman -S icu
```

### Display Issues

If the UI doesn't display correctly, try:

```bash
export AVALONIA_SCREEN_SCALE_FACTOR=1
./publish-avalonia/LisTube
```

## License

Same as original LisTube project.

## Credits

- Re-imagined by AmpliDEV.
- Avalonia UI version for cross-platform modern support.
