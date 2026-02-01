using System.Windows;
using LocalTTS.Services;

namespace LocalTTS;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        DockerImageBox.Text = settings.DockerImage;
        PortBox.Text = settings.Port.ToString();
        ContainerNameBox.Text = settings.ContainerName;
        VoiceBox.Text = settings.Voice;
        AutoStartBox.IsChecked = settings.AutoStartContainer;
        AutoStopBox.IsChecked = settings.AutoStopContainer;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortBox.Text, out var port) || port < 1 || port > 65535)
        {
            MessageBox.Show("Port must be a number between 1 and 65535.", "Invalid Port",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.DockerImage = DockerImageBox.Text.Trim();
        _settings.Port = port;
        _settings.ContainerName = ContainerNameBox.Text.Trim();
        _settings.Voice = VoiceBox.Text.Trim();
        _settings.AutoStartContainer = AutoStartBox.IsChecked == true;
        _settings.AutoStopContainer = AutoStopBox.IsChecked == true;
        _settings.Save();

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
