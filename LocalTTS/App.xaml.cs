using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using LocalTTS.Services;

namespace LocalTTS;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private HotkeyService? _hotkeyService;
    private AudioPlayerService? _audioPlayer;
    private TtsService? _ttsService;
    private DockerService? _dockerService;
    private AppSettings _settings = new();
    private bool _isProcessing;

    // Reader View state
    private DateTime _lastHotkeyPress = DateTime.MinValue;
    private CancellationTokenSource? _hotkeyDelayCancel;
    private ReaderWindow? _readerWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Info("App starting...");

        _settings = AppSettings.Load();
        _audioPlayer = new AudioPlayerService();
        _ttsService = new TtsService(_settings);
        _dockerService = new DockerService(_settings);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "LocalTTS - Starting...",
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenu = CreateContextMenu()
        };

        // Try to load icon
        try
        {
            var iconUri = new Uri("pack://application:,,,/Resources/icon.ico");
            var iconStream = GetResourceStream(iconUri)?.Stream;
            if (iconStream != null)
                _trayIcon.Icon = new System.Drawing.Icon(iconStream);
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.Register();

        _trayIcon.ToolTipText = "LocalTTS - Starting Kokoro...";
        try
        {
            await _dockerService.EnsureRunningAsync();
            _trayIcon.ToolTipText = "LocalTTS - Ready (Ctrl+Shift+R)";
            _trayIcon.ShowBalloonTip("LocalTTS", "Ready! Highlight text and press Ctrl+Shift+R", BalloonIcon.Info);
            Log.Info("Startup complete - ready");
        }
        catch (Exception ex)
        {
            Log.Error("Docker startup failed", ex);
            _trayIcon.ToolTipText = "LocalTTS - Docker error";
            _trayIcon.ShowBalloonTip("LocalTTS", $"Docker error: {ex.Message}", BalloonIcon.Error);
        }
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings..." };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var logItem = new System.Windows.Controls.MenuItem { Header = "View Log..." };
        logItem.Click += (_, _) => OpenLog();
        menu.Items.Add(logItem);

        var stopItem = new System.Windows.Controls.MenuItem { Header = "Stop Playback" };
        stopItem.Click += (_, _) => _audioPlayer?.Stop();
        menu.Items.Add(stopItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private LogWindow? _logWindow;

    private void OpenLog()
    {
        if (_logWindow is { IsLoaded: true })
        {
            _logWindow.Activate();
            return;
        }
        _logWindow = new LogWindow();
        _logWindow.Show();
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settings);
        if (window.ShowDialog() == true)
        {
            _ttsService = new TtsService(_settings);
            _dockerService = new DockerService(_settings);
            Log.Info("Settings updated");
        }
    }

    private async void OnHotkeyPressed()
    {
        // If audio is playing, stop it regardless of double-press
        if (_audioPlayer?.IsPlaying == true)
        {
            _audioPlayer.Stop();
            return;
        }

        var now = DateTime.Now;
        var timeSinceLastPress = (now - _lastHotkeyPress).TotalMilliseconds;
        _lastHotkeyPress = now;

        Log.Info($"Hotkey pressed. Time since last: {timeSinceLastPress:F0}ms, ReaderEnabled: {_settings.ReaderViewEnabled}");

        // Cancel any pending single-press action
        _hotkeyDelayCancel?.Cancel();

        if (_settings.ReaderViewEnabled && timeSinceLastPress < _settings.DoublePressTimeoutMs)
        {
            // Double press detected - open reader
            Log.Info("Double-press detected - opening reader");
            OpenReaderView();
        }
        else if (_settings.ReaderViewEnabled)
        {
            // Wait to see if this becomes a double press
            Log.Info($"Waiting {_settings.DoublePressTimeoutMs}ms for potential double-press...");
            _hotkeyDelayCancel = new CancellationTokenSource();
            try
            {
                await Task.Delay(_settings.DoublePressTimeoutMs, _hotkeyDelayCancel.Token);
                // No second press came - do TTS
                Log.Info("No double-press - performing TTS");
                await PerformTts();
            }
            catch (TaskCanceledException)
            {
                // Second press came - handled above
                Log.Info("Wait cancelled - second press detected");
            }
        }
        else
        {
            // Reader view disabled - immediate TTS
            Log.Info("Reader disabled - immediate TTS");
            await PerformTts();
        }
    }

    private async Task PerformTts()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            var text = TextCaptureService.CaptureSelectedText();
            if (string.IsNullOrWhiteSpace(text))
            {
                _trayIcon?.ShowBalloonTip("LocalTTS", "No text selected", BalloonIcon.Warning);
                return;
            }

            _trayIcon!.ToolTipText = "LocalTTS - Generating...";
            CursorIndicator.ShowBusy();
            var audioData = await _ttsService!.SynthesizeAsync(text);
            CursorIndicator.Restore();
            _audioPlayer!.Play(audioData);
            _trayIcon.ToolTipText = "LocalTTS - Ready (Ctrl+Shift+R)";
        }
        catch (Exception ex)
        {
            CursorIndicator.Restore();
            _trayIcon?.ShowBalloonTip("LocalTTS", $"TTS error: {ex.Message}", BalloonIcon.Error);
            _trayIcon!.ToolTipText = "LocalTTS - Ready (Ctrl+Shift+R)";
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void OpenReaderView()
    {
        Log.Info("OpenReaderView called");
        var text = TextCaptureService.CaptureSelectedText();
        if (string.IsNullOrWhiteSpace(text))
        {
            Log.Info("No text selected for reader");
            _trayIcon?.ShowBalloonTip("LocalTTS", "No text selected", BalloonIcon.Warning);
            return;
        }

        Log.Info($"Reader text captured: {text.Length} chars");
        var processor = new TextProcessor();
        var cleanedText = processor.Clean(text);

        // Reuse or create window
        if (_readerWindow is { IsLoaded: true })
        {
            Log.Info("Reusing existing reader window");
            _readerWindow.UpdateText(cleanedText);
            _readerWindow.Activate();
        }
        else
        {
            Log.Info("Creating new reader window");
            _readerWindow = new ReaderWindow(cleanedText, _settings, OnReaderPlayRequested);
            _readerWindow.Show();
            Log.Info("Reader window shown");
        }

        // Auto-play if enabled
        if (_settings.ReaderAutoPlay)
        {
            Log.Info("Auto-playing TTS");
            _audioPlayer?.Stop();
            _ = PerformTtsForText(cleanedText);
        }
    }

    private void OnReaderPlayRequested(string text)
    {
        _audioPlayer?.Stop();
        _ = PerformTtsForText(text);
    }

    private async Task PerformTtsForText(string text)
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            _trayIcon!.ToolTipText = "LocalTTS - Generating...";
            CursorIndicator.ShowBusy();
            var audioData = await _ttsService!.SynthesizeAsync(text);
            CursorIndicator.Restore();
            _audioPlayer!.Play(audioData);
            _trayIcon.ToolTipText = "LocalTTS - Ready (Ctrl+Shift+R)";
        }
        catch (Exception ex)
        {
            CursorIndicator.Restore();
            _trayIcon?.ShowBalloonTip("LocalTTS", $"TTS error: {ex.Message}", BalloonIcon.Error);
            _trayIcon!.ToolTipText = "LocalTTS - Ready (Ctrl+Shift+R)";
        }
        finally
        {
            _isProcessing = false;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Unregister();
        _audioPlayer?.Stop();
        _trayIcon?.Dispose();

        if (_dockerService != null)
        {
            try { await _dockerService.StopAsync(); } catch { }
        }

        base.OnExit(e);
    }
}
