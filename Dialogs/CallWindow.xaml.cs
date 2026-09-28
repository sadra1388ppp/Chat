using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Chat.Models;
using Chat.Services;

namespace Chat.Dialogs;

public partial class CallWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _stopwatch = new();
    private bool _isMuted;
    private bool _speakerOn = true;

    public CallWindow(ChatConversation conversation, ChatCharacter contact)
    {
        InitializeComponent();

        NameText.Text = contact.Name;
        AvatarText.Text = string.IsNullOrWhiteSpace(contact.AvatarIcon)
            ? contact.Initial
            : contact.AvatarIcon;

        if (!string.IsNullOrWhiteSpace(contact.AvatarColor))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(contact.AvatarColor);
                AvatarCircle.Background = new SolidColorBrush(color);
            }
            catch
            {
                // Keep the theme accent.
            }
        }

        StatusText.Text = "Ready to call";
        ConnectionText.Text = "Choose a scenario, then start your practice call.";
        ScenarioCombo.SelectedIndex = 0;

        MuteButton.IsEnabled = false;
        SpeakerButton.IsEnabled = false;

        _timer.Tick += (_, _) =>
        {
            var elapsed = _stopwatch.Elapsed;
            TimerText.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        };
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        ScenarioCombo.IsEnabled = false;
        MuteButton.IsEnabled = true;
        SpeakerButton.IsEnabled = true;
        RecapText.Visibility = Visibility.Collapsed;

        StatusText.Text = "Connecting…";
        ConnectionText.Text = "Setting up your practice call…";

        await Task.Delay(650);

        if (!IsVisible)
            return;

        StatusText.Text = "Practice call in progress";
        ConnectionText.Text = "Call connected";
        EndButton.Visibility = Visibility.Visible;
        StartButton.Visibility = Visibility.Collapsed;

        _stopwatch.Restart();
        _timer.Start();

        StartPulse();
    }

    private void End_Click(object sender, RoutedEventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;

        StopCall();

        var scenario = (ScenarioCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)
            ?.Content?.ToString() ?? "practice";

        var duration = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        RecapText.Text =
            $"Practice complete • {scenario}\nDuration: {duration}\n\nUse the next call to rehearse the same situation again.";
        RecapText.Visibility = Visibility.Visible;
        StatusText.Text = "Call complete";
        ConnectionText.Text = "Your practice recap is ready.";

        EndButton.Visibility = Visibility.Collapsed;
        StartButton.Visibility = Visibility.Visible;
        StartButton.IsEnabled = true;
        ScenarioCombo.IsEnabled = true;
        MuteButton.IsEnabled = false;
        SpeakerButton.IsEnabled = false;
        StopPulse();
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        MuteButton.Opacity = _isMuted ? 1 : 0.65;
        MuteButton.ToolTip = _isMuted ? "Unmute" : "Mute";
        ConnectionText.Text = _isMuted
            ? "Microphone muted • practice call continues"
            : "Call connected";
    }

    private void Speaker_Click(object sender, RoutedEventArgs e)
    {
        _speakerOn = !_speakerOn;
        SpeakerButton.Opacity = _speakerOn ? 1 : 0.65;
        SpeakerButton.ToolTip = _speakerOn ? "Turn speaker off" : "Turn speaker on";
        ConnectionText.Text = _speakerOn
            ? "Call connected • speaker on"
            : "Call connected • speaker off";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void StopCall()
    {
        _timer.Stop();
        _stopwatch.Stop();
        StopPulse();
    }

    private void StartPulse()
    {
        var animation = new DoubleAnimation
        {
            From = 0.12,
            To = 0.42,
            Duration = TimeSpan.FromSeconds(1.2),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        OuterRing.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private void StopPulse()
    {
        OuterRing.BeginAnimation(UIElement.OpacityProperty, null);
        OuterRing.Opacity = 0.15;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopCall();
        base.OnClosed(e);
    }

    private static SolidColorBrush ThemeBrush(string key) =>
        Application.Current.Resources[key] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Transparent);
}
