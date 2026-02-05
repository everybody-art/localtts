using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LocalTTS.Services;

namespace LocalTTS;

public partial class ReaderWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<string>? _onPlayRequested;
    private string _currentText = string.Empty;
    private int _fontSize;
    private bool _hasBeenActivated;
    private bool _isClosing;

    public ReaderWindow(string text, AppSettings settings, Action<string>? onPlayRequested = null)
    {
        InitializeComponent();
        _settings = settings;
        _onPlayRequested = onPlayRequested;
        _fontSize = settings.ReaderFontSize;

        ApplyTheme();
        UpdateText(text);

        // Only close on deactivate AFTER window has been activated once
        Activated += OnActivated;
        Deactivated += OnDeactivated;
        KeyDown += OnKeyDown;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        _hasBeenActivated = true;
    }

    private void OnToolbarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    public void UpdateText(string text)
    {
        _currentText = text;
        Document.Blocks.Clear();

        // Split into paragraphs and add to document
        var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var para in paragraphs)
        {
            var paragraph = new Paragraph(new Run(para.Replace("\n", " ")))
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            Document.Blocks.Add(paragraph);
        }

        ApplyFontSettings();
    }

    private void ApplyTheme()
    {
        if (_settings.ReaderDarkMode)
        {
            Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            Resources["ToolbarBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            Resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        }
        else
        {
            Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Resources["ToolbarBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            Resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
        }
    }

    private void ApplyFontSettings()
    {
        Document.FontFamily = new FontFamily(_settings.ReaderFontFamily);
        Document.FontSize = _fontSize;
        FontSizeDisplay.Text = _fontSize.ToString();
    }

    private void OnReadAloud(object sender, RoutedEventArgs e)
    {
        _onPlayRequested?.Invoke(_currentText);
    }

    private void OnFontDecrease(object sender, RoutedEventArgs e)
    {
        if (_fontSize > 10)
        {
            _fontSize -= 2;
            ApplyFontSettings();
        }
    }

    private void OnFontIncrease(object sender, RoutedEventArgs e)
    {
        if (_fontSize < 36)
        {
            _fontSize += 2;
            ApplyFontSettings();
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            Close();
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Only close if we've been activated at least once and not already closing
        if (_hasBeenActivated && !_isClosing)
        {
            _isClosing = true;
            Close();
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_isClosing)
        {
            _isClosing = true;
            Close();
        }
    }
}
