using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Chat.Models;

namespace Chat.Dialogs;

public partial class CallWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _stopwatch = new();

    public CallWindow(ChatConversation conversation, ChatCharacter contact)
    {
        InitializeComponent();

        NameText.Text = contact.Name;
        AvatarText.Text = contact.Initial;
        StatusText.Text = contact.IsAi ? "AI practice contact" : "Practice contact";
        ScenarioCombo.SelectedIndex = 0;

        _timer.Tick += (_, _) =>
        {
            var elapsed = _stopwatch.Elapsed;
            TimerText.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        };
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        EndButton.IsEnabled = true;
        ScenarioCombo.IsEnabled = false;
        StatusText.Text = "Practice call in progress";
        _stopwatch.Restart();
        _timer.Start();
        RecapText.Visibility = Visibility.Collapsed;
    }

    private void End_Click(object sender, RoutedEventArgs e)
    {
        StopCall();
        var scenario = (ScenarioCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Practice";
        RecapText.Text = $"Recap: you completed a {scenario.ToLowerInvariant()} practice call lasting {TimerText.Text}.";
        RecapText.Visibility = Visibility.Visible;
        StatusText.Text = "Call complete";
    }

    private void StopCall()
    {
        _timer.Stop();
        _stopwatch.Stop();
        EndButton.IsEnabled = false;
        StartButton.IsEnabled = true;
        ScenarioCombo.IsEnabled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopCall();
        base.OnClosed(e);
    }
}