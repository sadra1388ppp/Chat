using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Chat.Models;
using Chat.Services;

namespace Chat.Dialogs;

public partial class CallWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _waveTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly Stopwatch _stopwatch = new();

    private Rectangle[] _waveBars = [];
    private double _wavePhase;
    private bool _isMuted;
    private bool _speakerOn = true;

    public CallWindow(ChatConversation conversation, ChatCharacter contact)
    {
        InitializeComponent();

        NameText.Text = contact.Name;
        PhoneText.Text = contact.PhoneNumber;
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
        TimerText.Text = "00:00";
        CallModeText.Text = "Ready to start";
        ConnectionText.Text = "Choose a scenario, then start the practice call.";
        ScenarioCombo.SelectedIndex = 0;

        MuteButton.IsEnabled = false;
        SpeakerButton.IsEnabled = false;

        _waveBars =
        [
            Wave1, Wave2, Wave3, Wave4, Wave5,
            Wave6, Wave7, Wave8, Wave9
        ];

        _timer.Tick += (_, _) =>
        {
            var elapsed = _stopwatch.Elapsed;
            TimerText.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        };

        _waveTimer.Tick += (_, _) => UpdateWaveform();

        SetReadyVisuals();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        ScenarioCombo.IsEnabled = false;
        MuteButton.IsEnabled = true;
        SpeakerButton.IsEnabled = true;
        RecapText.Visibility = Visibility.Collapsed;

        StatusText.Text = "Connecting…";
        CallModeText.Text = "Connecting";
        ConnectionText.Text = "Setting up your practice call…";

        await Task.Delay(650);

        if (!IsVisible)
            return;

        StatusText.Text = "Connected";
        CallModeText.Text = "Live practice session";
        ConnectionText.Text = "Call connected • microphone ready";
        EndButton.Visibility = Visibility.Visible;
        StartButton.Visibility = Visibility.Collapsed;
        ScenarioPanel.Visibility = Visibility.Collapsed;
        WaveformPanel.Visibility = Visibility.Visible;

        _stopwatch.Restart();
        _timer.Start();
        _waveTimer.Start();

        StartPulse();
    }

    private void End_Click(object sender, RoutedEventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;

        StopCall();

        var scenario = (ScenarioCombo.SelectedItem as ComboBoxItem)
            ?.Content?.ToString() ?? "practice";

        var duration = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        RecapText.Text =
            $"Practice complete • {scenario}\nDuration: {duration}\n\nUse the next call to rehearse the same situation again.";
        RecapText.Visibility = Visibility.Visible;

        StatusText.Text = "Call ended";
        CallModeText.Text = "Practice complete";
        ConnectionText.Text = "Your recap is ready.";

        EndButton.Visibility = Visibility.Collapsed;
        StartButton.Visibility = Visibility.Visible;
        StartButton.IsEnabled = true;
        ScenarioCombo.IsEnabled = true;
        ScenarioPanel.Visibility = Visibility.Visible;
        MuteButton.IsEnabled = false;
        SpeakerButton.IsEnabled = false;

        SetReadyVisuals();
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        MuteButton.Opacity = _isMuted ? 1 : 0.66;
        MuteButton.ToolTip = _isMuted ? "Unmute" : "Mute";
        ConnectionText.Text = _isMuted
            ? "Call connected • microphone muted"
            : "Call connected • microphone ready";
    }

    private void Speaker_Click(object sender, RoutedEventArgs e)
    {
        _speakerOn = !_speakerOn;
        SpeakerButton.Opacity = _speakerOn ? 1 : 0.66;
        SpeakerButton.ToolTip = _speakerOn ? "Turn speaker off" : "Turn speaker on";
        ConnectionText.Text = _speakerOn
            ? "Call connected • speaker on"
            : "Call connected • speaker off";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void StopCall()
    {
        _timer.Stop();
        _waveTimer.Stop();
        _stopwatch.Stop();
        StopPulse();
    }

    private void SetReadyVisuals()
    {
        WaveformPanel.Opacity = 0.35;
        _wavePhase = 0;

        foreach (var bar in _waveBars)
            bar.Height = 8;

        MuteButton.Opacity = 0.5;
        SpeakerButton.Opacity = 0.5;
        _speakerOn = true;
        _isMuted = false;
        MuteButton.ToolTip = "Mute";
        SpeakerButton.ToolTip = "Speaker";
    }

    private void UpdateWaveform()
    {
        _wavePhase += 0.38;

        for (var i = 0; i < _waveBars.Length; i++)
        {
            var value = Math.Abs(Math.Sin(_wavePhase + i * 0.72));
            _waveBars[i].Height = 8 + (24 * value);
        }

        WaveformPanel.Opacity = 0.55 + (0.35 * Math.Abs(Math.Sin(_wavePhase * 0.5)));
    }

    private void StartPulse()
    {
        var animation = new DoubleAnimation
        {
            From = 0.13,
            To = 0.46,
            Duration = TimeSpan.FromSeconds(1.1),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        OuterRing.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private void StopPulse()
    {
        OuterRing.BeginAnimation(UIElement.OpacityProperty, null);
        OuterRing.Opacity = 0.16;
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