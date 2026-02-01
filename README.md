# LocalTTS

A Windows system tray app that reads highlighted text aloud using [Kokoro TTS](https://github.com/remsky/Kokoro-FastAPI) running locally via Docker.

## How It Works

1. Highlight text in any application
2. Press **Ctrl+Shift+R** to hear it spoken
3. Press **Ctrl+Shift+R** again to stop playback

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Setup

```
git clone https://github.com/everybody-art/localtts.git
cd localtts
dotnet run --project LocalTTS
```

On first launch the app will pull and start the Kokoro TTS Docker container (`ghcr.io/remsky/kokoro-fastapi-cpu:latest`). This may take a few minutes the first time. A tray icon will appear and show "Ready" when the TTS engine is available.

## Architecture

```
Global hotkey (Ctrl+Shift+R)
  -> Copies selected text via simulated Ctrl+C
  -> POST to Kokoro-FastAPI (localhost:8880)
  -> Plays audio via NAudio
```

The app automatically manages the Docker container lifecycle — starting it on launch and stopping it on exit.

## Configuration

The default voice is `af_heart`. To change it, edit `voice` in `LocalTTS/Services/TtsService.cs`.

## Troubleshooting

Logs are written to `localtts.log` in the application's output directory (e.g. `LocalTTS/bin/Debug/net8.0-windows/localtts.log`).

Common issues:
- **"Starting Kokoro..." stays forever**: Check that Docker Desktop is running and `docker ps` works from your terminal
- **"No text selected"**: Make sure you have text highlighted before pressing the hotkey
- **No tray icon visible**: Check the system tray overflow area (^ arrow in taskbar)
