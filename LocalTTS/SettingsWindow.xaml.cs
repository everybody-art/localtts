using System.Windows;
using System.Windows.Controls;
using LocalTTS.Services;

namespace LocalTTS;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        // Docker settings
        DockerImageBox.Text = settings.DockerImage;
        PortBox.Text = settings.Port.ToString();
        ContainerNameBox.Text = settings.ContainerName;
        VoiceBox.Text = settings.Voice;
        AutoStartBox.IsChecked = settings.AutoStartContainer;
        AutoStopBox.IsChecked = settings.AutoStopContainer;

        // Reader View settings
        ShowReaderWindowBox.IsChecked = settings.ShowReaderWindow;
        ReaderAutoPlayBox.IsChecked = settings.ReaderAutoPlay;
        ReaderDarkModeBox.IsChecked = settings.ReaderDarkMode;
        ReaderFontSizeBox.Text = settings.ReaderFontSize.ToString();

        // Set font family selection
        foreach (ComboBoxItem item in ReaderFontBox.Items)
        {
            if (item.Content.ToString() == settings.ReaderFontFamily)
            {
                ReaderFontBox.SelectedItem = item;
                break;
            }
        }
        if (ReaderFontBox.SelectedItem == null)
            ReaderFontBox.SelectedIndex = 0;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortBox.Text, out var port) || port < 1 || port > 65535)
        {
            MessageBox.Show("Port must be a number between 1 and 65535.", "Invalid Port",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(ReaderFontSizeBox.Text, out var fontSize) || fontSize < 10 || fontSize > 36)
        {
            MessageBox.Show("Font size must be a number between 10 and 36.", "Invalid Font Size",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Docker settings
        _settings.DockerImage = DockerImageBox.Text.Trim();
        _settings.Port = port;
        _settings.ContainerName = ContainerNameBox.Text.Trim();
        _settings.Voice = VoiceBox.Text.Trim();
        _settings.AutoStartContainer = AutoStartBox.IsChecked == true;
        _settings.AutoStopContainer = AutoStopBox.IsChecked == true;

        // Reader View settings
        _settings.ShowReaderWindow = ShowReaderWindowBox.IsChecked == true;
        _settings.ReaderAutoPlay = ReaderAutoPlayBox.IsChecked == true;
        _settings.ReaderDarkMode = ReaderDarkModeBox.IsChecked == true;
        _settings.ReaderFontSize = fontSize;
        _settings.ReaderFontFamily = (ReaderFontBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Segoe UI";

        _settings.Save();

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
