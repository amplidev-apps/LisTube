# LisTube 2.0

**LisTube** is an advanced multi-platform desktop application for downloading playlists, videos, and audio from YouTube. Entirely re-imagined from the ground up by **AmpliDEV**.

Built with **Avalonia UI**, it runs natively on **Linux, macOS, and Windows**.

---

## Features

- **Multi-Format Downloads** — MP4 video (high quality), MP3 audio (320kbps or standard), and M4A/AAC native audio
- **Playlist & Channel Support** — Load entire playlists or channels, select which videos to download
- **Single Video Support** — Supports `youtube.com/watch`, `youtu.be`, `/live/`, and `/shorts/` URLs
- **Batch Download Queue** — Add multiple playlists/channels to a queue with up to 20 simultaneous downloads
- **Audio Splitting** — Split a single audio file into segments by timestamp with custom naming
- **Real-Time Progress** — Live download progress with speed, size, and ETA display
- **Authentication Support** — Cookie-based authentication for age-restricted or private videos (manual cookie import or auto-detect from Chrome/Brave/Firefox/Chromium/Edge)
- **Smart Format Selection** — Automatic best-quality format selection via yt-dlp
- **Cancel Downloads** — Cancel in-progress downloads at any time
- **Configurable Save Paths** — Separate output directories for audio and video
- **Multi-Language Support** — English, Arabic, Chinese, Dutch, French, German, Hebrew, Italian, Polish, Portuguese (BR), Romanian, Russian, Spanish, Turkish
- **Cross-Platform** — Native on Linux, macOS, and Windows (no emulation)

---

## Requirements

### Runtime

- **yt-dlp** (required) — Download from [yt-dlp GitHub](https://github.com/yt-dlp/yt-dlp) or install via package manager
- **ffmpeg** (required for audio splitting) — Install via package manager
- **Node.js** (optional, improves YouTube signature solving) — Download from [nodejs.org](https://nodejs.org)

### Build

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Linux, macOS, or Windows

---

## Installation

### Pre-built binary (Linux x64)

A self-contained executable is available in the `publish-avalonia/` directory:

```bash
chmod +x publish-avalonia/LisTube
./publish-avalonia/LisTube
```

Or use the run script:

```bash
./run-avalonia.sh
```

### Install required dependencies

```bash
# Ubuntu / Debian
sudo apt install yt-dlp ffmpeg

# Fedora
sudo dnf install yt-dlp ffmpeg

# Arch / Manjaro
sudo pacman -S yt-dlp ffmpeg

# macOS (Homebrew)
brew install yt-dlp ffmpeg

# Windows (winget)
winget install yt-dlp ffmpeg
```

### Build from source

#### Option 1: Build Script (recommended)

```bash
./build-avalonia.sh
```

The script automatically installs the .NET SDK if missing and builds the application. The output goes to `publish-avalonia/`.

#### Option 2: Manual Build

```bash
# Restore dependencies
cd LisTube.Avalonia
dotnet restore

# Build
dotnet build --configuration Release

# Run directly
dotnet run

# Or publish a self-contained executable
dotnet publish --configuration Release --self-contained --runtime linux-x64 -o ../publish-avalonia
```

---

## Usage

### Basic Download Flow

1. Launch LisTube
2. Paste a YouTube URL (video, playlist, channel, shorts, or live link)
3. Click **Load** to fetch video information
4. Select the videos you want to download
5. Choose a format:
   - **Vídeo (Alta Qualidade MP4)** — Best quality MP4 video
   - **Áudio (MP3 320kbps)** — High quality MP3
   - **Áudio (MP3 Padrão)** — Standard MP3
   - **Áudio (M4A / AAC Nativo)** — Native AAC (smaller files)
6. Click **Download**

### Audio Splitting

For single audio downloads, enable **Split** and enter timestamps:

```
0:00 - Intro
1:30 - Verse One
3:45 - Chorus
5:20 - Bridge
```

Each line must follow the format: `MM:SS - Segment Name` or `HH:MM:SS - Segment Name`.

### Batch Queue

For multiple playlists/channels:
1. Load a playlist and click **Add to Queue**
2. Repeat for additional playlists
3. Navigate to the **Queue** tab to monitor progress

### Authentication (Cookies)

If a video is age-restricted or requires login:

1. Install a browser extension to export cookies (e.g., "Get cookies.txt" for Chrome)
2. Log in to YouTube in your browser
3. Export cookies in Netscape format
4. Open LisTube **Settings** → paste the cookie contents
5. LisTube will also auto-detect cookies from installed browsers (Chrome, Brave, Firefox, Chromium, Edge)

---

## Project Structure

```
LisTube/
├── LisTube.Avalonia/          # Cross-platform Avalonia UI app
│   ├── Assets/Languages/      # Localization files
│   ├── Models/                # Data models
│   │   ├── VideoItem.cs       # Video metadata
│   │   ├── PlaylistDownloadTask.cs  # Queue task model
│   │   └── SplitSegment.cs    # Audio split segment
│   ├── Services/              # Business logic
│   │   ├── YtDlpService.cs    # yt-dlp integration
│   │   ├── AudioSplitter.cs   # ffmpeg audio splitting
│   │   └── YoutubeClientFactory.cs  # YouTube API client
│   ├── ViewModels/            # MVVM ViewModels
│   │   ├── MainPageViewModel.cs   # Main download page
│   │   ├── QueueViewModel.cs      # Download queue
│   │   ├── SettingsViewModel.cs   # App settings
│   │   ├── AboutViewModel.cs
│   │   └── HelpViewModel.cs
│   ├── Views/                 # Avalonia XAML views
│   ├── Styles/                # Theme and styling
│   ├── DownloadQueueManager.cs     # Queue concurrency (max 20)
│   ├── AppSettings.cs              # Global settings
│   └── App.axaml / Program.cs      # App entry point
├── LisTube/                   # Legacy WPF version (Windows only)
├── publish-avalonia/          # Pre-built Linux x64 executable
├── build-avalonia.sh          # Build script
├── run-avalonia.sh            # Run script
└── README.md
```

---

## Technologies

| Technology | Purpose |
|---|---|
| **Avalonia UI 11.0** | Cross-platform UI framework |
| **.NET 8.0** | Runtime and SDK |
| **CommunityToolkit.Mvvm** | MVVM source generators |
| **YoutubeExplode 6.6** | YouTube API client (metadata) |
| **yt-dlp** | Video/audio download engine |
| **ffmpeg** | Audio splitting |
| **TagLib#** | Audio file tagging |

---

## Download Save Paths

By default, files are saved to:

- **Audio**: `~/Downloads/LisTube/Audio/`
- **Video**: `~/Downloads/LisTube/Video/`

These paths can be changed in **Settings**.

---

## Troubleshooting

### yt-dlp not found

Install yt-dlp:
```bash
# Linux
sudo apt install yt-dlp          # Debian/Ubuntu
sudo dnf install yt-dlp          # Fedora
sudo pacman -S yt-dlp            # Arch

# macOS
brew install yt-dlp

# Windows
winget install yt-dlp
```

### ffmpeg not found (audio splitting)

```bash
# Linux
sudo apt install ffmpeg          # Debian/Ubuntu
sudo dnf install ffmpeg          # Fedora
sudo pacman -S ffmpeg            # Arch

# macOS
brew install ffmpeg

# Windows
winget install ffmpeg
```

### Missing libicu

```bash
# Ubuntu/Debian
sudo apt install libicu-dev

# Fedora
sudo dnf install libicu

# Arch
sudo pacman -S icu
```

### Display / Scaling issues

```bash
export AVALONIA_SCREEN_SCALE_FACTOR=1
./publish-avalonia/LisTube
```

### Files don't appear after download (Windows)

If files don't show up after downloading, the save directory may be on a protected drive. Change the save path in **Settings** to a different drive (e.g., D:\ or E:\).

---

## Comparison: Avalonia vs Legacy WPF

| Feature | Avalonia Version | WPF Version |
|---|---|---|
| Platform | Linux, macOS, Windows | Windows only |
| UI Framework | Avalonia UI | WPF |
| Updates | Manual | Auto-update |
| Maintenance | Active | Legacy |

---

## License

Licensed under the [Apache License, Version 2.0](LICENSE).

Copyright 2024 AmpliDEV.

---

## Credits

- **AmpliDEV** — Re-imagined and developed
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — Download engine
- [Avalonia UI](https://avaloniaui.net/) — Cross-platform UI framework
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) — YouTube API client
