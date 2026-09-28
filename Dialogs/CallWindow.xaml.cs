using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Chat.Models;

namespace Chat.Dialogs;

public partial class CallWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _stopwatch = new();

    private bool _callConnected;
    private bool _callActive;
    private bool _isMuted;
    private bool _speakerOn = true;

    public CallWindow(ChatConversation conversation, ChatCharacter contact)
    {
        InitializeComponent();

        NameText.Text = contact.Name;
        PhoneText.Text = string.IsNullOrWhiteSpace(contact.PhoneNumber)
            ? "Voice call"
            : contact.PhoneNumber;

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

        _timer.Tick += (_, _) =>
        {
            if (!_callConnected)
                return;

            var elapsed = _stopwatch.Elapsed;
            TimerText.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        };

        Loaded += async (_, _) => await BeginCallAsync();
    }

    private async Task BeginCallAsync()
    {
        if (_callActive)
            return;

        _callActive = true;
        _callConnected = false;
        _isMuted = false;
        _speakerOn = true;
        MuteButton.Opacity = 1;
        SpeakerButton.Opacity = 1;
        MuteButton.ToolTip = "Mute microphone";
        SpeakerButton.ToolTip = "Turn speaker off";

        CallModeText.Text = "Outgoing call";
        StatusText.Text = "Calling…";
        ConnectionText.Text = "Connecting securely…";
        TimerText.Text = "00:00";

        MuteButton.IsEnabled = false;
        SpeakerButton.IsEnabled = false;
        EndButton.IsEnabled = true;
        AgainPanel.Visibility = Visibility.Collapsed;

        StartPulse();

        await Task.Delay(1100);

        if (!IsVisible || !_callActive)
            return;

        _callConnected = true;
        StatusText.Text = "Connected";
        CallModeText.Text = "Live call";
        ConnectionText.Text = "Connected • microphone ready";

        MuteButton.IsEnabled = true;
        SpeakerButton.IsEnabled = true;

        _stopwatch.Restart();
        _timer.Start();
    }

    private void End_Click(object sender, RoutedEventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;
        var wasConnected = _callConnected;

        StopCall();

        StatusText.Text = "Call ended";
        CallModeText.Text = "Call complete";
        ConnectionText.Text = wasConnected
            ? $"Call ended • {FormatDuration(elapsed)}"
            : "Call ended before connection";

        EndButton.IsEnabled = false;
        MuteButton.IsEnabled = false;
        SpeakerButton.IsEnabled = false;
        AgainPanel.Visibility = Visibility.Visible;

        StopPulse();
    }

    private async void CallAgain_Click(object sender, RoutedEventArgs e)
    {
        await BeginCallAsync();
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        if (!_callConnected)
            return;

        _isMuted = !_isMuted;
        MuteButton.Opacity = _isMuted ? 1 : 0.72;
        MuteButton.ToolTip = _isMuted ? "Unmute microphone" : "Mute microphone";
        MuteLabelText(_isMuted);

        ConnectionText.Text = _isMuted
            ? "Connected • microphone muted"
            : "Connected • microphone ready";
    }

    private void Speaker_Click(object sender, RoutedEventArgs e)
    {
        if (!_callConnected)
            return;

        _speakerOn = !_speakerOn;
        SpeakerButton.Opacity = _speakerOn ? 1 : 0.72;
        SpeakerButton.ToolTip = _speakerOn ? "Turn speaker off" : "Turn speaker on";

        ConnectionText.Text = _speakerOn
            ? "Connected • speaker on"
            : "Connected • speaker off";
    }

    private void MuteLabelText(bool muted)
    {
        MuteLabel.Text = muted ? "Muted" : "Mute";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void StopCall()
    {
        _callActive = false;
        _callConnected = false;
        _timer.Stop();
        _stopwatch.Stop();
        TimerText.Text = FormatDuration(_stopwatch.Elapsed);
    }

    private static string FormatDuration(TimeSpan elapsed) =>
        $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";

    private void StartPulse()
    {
        var animation = new DoubleAnimation
        {
            From = 0.12,
            To = 0.42,
            Duration = TimeSpan.FromSeconds(1.15),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        OuterRing.BeginAnimation(UIElement.OpacityProperty, animation);
        ConnectionDot.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private void StopPulse()
    {
        OuterRing.BeginAnimation(UIElement.OpacityProperty, null);
        ConnectionDot.BeginAnimation(UIElement.OpacityProperty, null);
        OuterRing.Opacity = 0.14;
        ConnectionDot.Opacity = 1;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopCall();
        base.OnClosed(e);
    }
}
