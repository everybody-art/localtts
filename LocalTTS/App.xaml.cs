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
    private bool _isProcessing;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Info("App starting...");

        _audioPlayer = new AudioPlayerService();
        _ttsService = new TtsService();
        _dockerService = new DockerService();

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
            // Use default icon if resource not found
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

        var stopItem = new System.Windows.Controls.MenuItem { Header = "Stop Playback" };
        stopItem.Click += (_, _) => _audioPlayer?.Stop();
        menu.Items.Add(stopItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private async void OnHotkeyPressed()
    {
        if (_audioPlayer?.IsPlaying == true)
        {
            _audioPlayer.Stop();
            return;
        }

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

            _trayIcon!.ToolTipText = "LocalTTS - Speaking...";
            var audioData = await _ttsService!.SynthesizeAsync(text);
            _audioPlayer!.Play(audioData);
            _trayIcon.ToolTipText = "LocalTTS - Ready (Ctrl+Shift+R)";
        }
        catch (Exception ex)
        {
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
