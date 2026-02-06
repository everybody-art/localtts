using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LocalTTS.Services;
using System.Text;

namespace LocalTTS;

public partial class ReaderWindow : Window {
    private const int MinFontSize = 10;
    private const int MaxFontSize = 36;
    private const int HighlightIntervalMs = 50;
    private const int ParagraphSpacing = 12;

    private readonly AppSettings _settings;
    private readonly Action<string>? _onPlayRequested;
    private readonly Action? _onClosed;
    private string _currentText = string.Empty;
    private int _fontSize;
    private bool _hasBeenActivated;
    private bool _isClosing;

    // Highlighting support
    private readonly List<Run> _wordRuns = [];
    private List<WordTimestamp>? _timestamps;
    private Func<double>? _getPlaybackPosition;
    private DispatcherTimer? _highlightTimer;
    private int _currentHighlightIndex = -1;
    private SolidColorBrush? _highlightBrush;
    private List<int>? _timestampRunMap;

    public ReaderWindow(string text, AppSettings settings, Action<string>? onPlayRequested = null, Action? onClosed = null) {
        InitializeComponent();
        _settings = settings;
        _onPlayRequested = onPlayRequested;
        _onClosed = onClosed;
        _fontSize = settings.ReaderFontSize;

        ApplyTheme();
        UpdateText(text);

        Activated += OnActivated;
        Deactivated += OnDeactivated;
        Closed += OnWindowClosed;
        KeyDown += OnKeyDown;
    }

    private void OnActivated(object? sender, EventArgs e) => _hasBeenActivated = true;

    private void OnDeactivated(object? sender, EventArgs e) {
        if (_settings.ReaderCloseOnFocusLoss && _hasBeenActivated && !_isClosing) {
            _isClosing = true;
            Close();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e) {
        StopHighlighting();
        _onClosed?.Invoke();
    }

    private void OnToolbarMouseDown(object sender, MouseButtonEventArgs e) {
        if (e.ChangedButton == MouseButton.Left) {
            DragMove();
        }
    }

    public void UpdateText(string text) {
        StopHighlighting();
        _currentText = text;
        _wordRuns.Clear();
        Document.Blocks.Clear();

        // Split into paragraphs
        var paragraphs = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var para in paragraphs) {
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, ParagraphSpacing) };
            var words = para.Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < words.Length; i++) {
                var run = new Run(words[i]);
                _wordRuns.Add(run);
                paragraph.Inlines.Add(run);

                // Add space between words (except last)
                if (i < words.Length - 1) {
                    paragraph.Inlines.Add(new Run(" "));
                }
            }

            Document.Blocks.Add(paragraph);
        }

        ApplyFontSettings();
    }

    public void StartHighlighting(List<WordTimestamp> timestamps, Func<double> getPlaybackPosition) {
        StopHighlighting();
        _timestamps = timestamps;
        _getPlaybackPosition = getPlaybackPosition;
        _currentHighlightIndex = -1;
        _timestampRunMap = BuildTimestampRunMap(timestamps, _wordRuns);

        _highlightTimer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(HighlightIntervalMs)
        };
        _highlightTimer.Tick += OnHighlightTick;
        _highlightTimer.Start();
    }

    public void StopHighlighting() {
        _highlightTimer?.Stop();
        _highlightTimer = null;
        _timestamps = null;
        _getPlaybackPosition = null;
        _timestampRunMap = null;
        ClearHighlight();
    }

    private static readonly string[] separator = new[] { "\n\n" };

    private void OnHighlightTick(object? sender, EventArgs e) {
        if (_timestamps == null || _getPlaybackPosition == null || _wordRuns.Count == 0) {
            return;
        }

        var position = _getPlaybackPosition();

        // Find current word based on position
        var newIndex = -1;
        for (var i = 0; i < _timestamps.Count; i++) {
            if (position >= _timestamps[i].StartTime && position < _timestamps[i].EndTime) {
                newIndex = i;
                break;
            }
        }

        if (newIndex == -1) {
            ClearHighlight();
            return;
        }

        var mappedIndex = newIndex;
        if (_timestampRunMap != null && newIndex >= 0 && newIndex < _timestampRunMap.Count) {
            mappedIndex = _timestampRunMap[newIndex];
        }

        if (mappedIndex == -1) {
            // Punctuation or unaligned token: keep current highlight
            return;
        }

        if (mappedIndex != _currentHighlightIndex) {
            // Clear previous highlight
            if (_currentHighlightIndex >= 0 && _currentHighlightIndex < _wordRuns.Count) {
                _wordRuns[_currentHighlightIndex].Background = Brushes.Transparent;
            }

            // Apply new highlight
            if (mappedIndex >= 0 && mappedIndex < _wordRuns.Count) {
                _wordRuns[mappedIndex].Background = _highlightBrush;
            }

            _currentHighlightIndex = mappedIndex;
        }
    }

    private void ClearHighlight() {
        if (_currentHighlightIndex >= 0 && _currentHighlightIndex < _wordRuns.Count) {
            _wordRuns[_currentHighlightIndex].Background = Brushes.Transparent;
        }
        _currentHighlightIndex = -1;
    }

    private void ApplyTheme() {
        if (_settings.ReaderDarkMode) {
            Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            Resources["ToolbarBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            Resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            _highlightBrush = new SolidColorBrush(Color.FromRgb(70, 100, 150));
        } else {
            Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Resources["ToolbarBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            Resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            _highlightBrush = new SolidColorBrush(Color.FromRgb(255, 235, 156)); // Yellow highlight
        }
    }

    private void ApplyFontSettings() {
        Document.FontFamily = new FontFamily(_settings.ReaderFontFamily);
        Document.FontSize = _fontSize;
        FontSizeDisplay.Text = _fontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnReadAloud(object sender, RoutedEventArgs e) => _onPlayRequested?.Invoke(_currentText);

    private void OnFontDecrease(object sender, RoutedEventArgs e) {
        if (_fontSize > MinFontSize) {
            _fontSize -= 2;
            ApplyFontSettings();
        }
    }

    private void OnFontIncrease(object sender, RoutedEventArgs e) {
        if (_fontSize < MaxFontSize) {
            _fontSize += 2;
            ApplyFontSettings();
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) {
        if (!_isClosing) {
            _isClosing = true;
            Close();
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e) {
        if (e.Key == Key.Escape && !_isClosing) {
            _isClosing = true;
            Close();
        }
    }

    // Greedy, forward-only token alignment between timestamps and visible runs.
    private static List<int> BuildTimestampRunMap(List<WordTimestamp> timestamps, List<Run> runs) {
        var map = new List<int>(timestamps.Count);
        var runIndex = 0;

        for (var i = 0; i < timestamps.Count; i++) {
            var tsNorm = NormalizeToken(timestamps[i].Word);
            if (string.IsNullOrEmpty(tsNorm)) {
                map.Add(-1);
                continue;
            }

            var matched = -1;
            while (runIndex < runs.Count) {
                var runNorm = NormalizeToken(runs[runIndex].Text);
                if (!string.IsNullOrEmpty(runNorm) && runNorm == tsNorm) {
                    matched = runIndex;
                    runIndex++;
                    break;
                }
                runIndex++;
            }

            map.Add(matched);
        }

        return map;
    }

    private static string NormalizeToken(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            return string.Empty;
        }

        var sb = new StringBuilder(token.Length);
        foreach (var ch in token) {
            var normalized = ch == '\u2019' ? '\'' : ch;
            if (char.IsLetterOrDigit(normalized) || normalized == '\'' || normalized == '-') {
                sb.Append(char.ToLowerInvariant(normalized));
            }
        }

        return sb.ToString();
    }
}
