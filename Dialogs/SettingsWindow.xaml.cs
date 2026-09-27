using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chat.Models;
using Chat.Services;

namespace Chat.Dialogs;

public partial class SettingsWindow : Window
{
    private readonly ChatConversation? _conversation;
    private readonly string _originalThemeId;

    public string SelectedThemeId { get; private set; }

    public SettingsWindow(ChatConversation? conversation)
    {
        InitializeComponent();

        _conversation = conversation;
        _originalThemeId = ThemeService.CurrentThemeId;
        SelectedThemeId = _originalThemeId;

        ReadReceiptsCheckBox.IsChecked = conversation?.ShowReadReceipts ?? true;
        TimestampCheckBox.IsChecked = conversation?.ShowTimestamps ?? true;
        TypingCheckBox.IsChecked = conversation?.ShowTypingIndicators ?? true;

        OutgoingColorBox.Text = conversation?.OutgoingBubbleColor
            ?? ThemeService.GetOutgoingBubbleColor(SelectedThemeId);

        IncomingColorBox.Text = conversation?.IncomingBubbleColor
            ?? ThemeService.GetIncomingBubbleColor(SelectedThemeId);

        RefreshThemeSelection();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        SelectedThemeId = ThemeService.Normalize(button.Tag?.ToString());
        ThemeService.ApplyTheme(SelectedThemeId);

        if (_conversation is null)
        {
            OutgoingColorBox.Text = ThemeService.GetOutgoingBubbleColor(SelectedThemeId);
            IncomingColorBox.Text = ThemeService.GetIncomingBubbleColor(SelectedThemeId);
        }

        RefreshThemeSelection();
    }

    private void RefreshThemeSelection()
    {
        foreach (var button in ThemeButtonsPanel.Children.OfType<Button>())
        {
            var selected = string.Equals(
                button.Tag?.ToString(),
                SelectedThemeId,
                StringComparison.OrdinalIgnoreCase);

            button.BorderBrush = selected
                ? ThemeBrush("Accent")
                : ThemeBrush("Divider");

            button.BorderThickness = selected
                ? new Thickness(2.5)
                : new Thickness(1);
            button.Background = selected
                ? ThemeBrush("AccentSoft")
                : ThemeBrush("PanelBackground");
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var outgoing = OutgoingColorBox.Text.Trim();
        var incoming = IncomingColorBox.Text.Trim();

        if (!IsValidColor(outgoing) || !IsValidColor(incoming))
        {
            MessageBox.Show(
                this,
                "Enter valid hex colors such as #0A84FF.",
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_conversation is not null)
        {
            _conversation.ShowReadReceipts = ReadReceiptsCheckBox.IsChecked == true;
            _conversation.ShowTimestamps = TimestampCheckBox.IsChecked == true;
            _conversation.ShowTypingIndicators = TypingCheckBox.IsChecked == true;
            _conversation.OutgoingBubbleColor = outgoing;
            _conversation.IncomingBubbleColor = incoming;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyTheme(_originalThemeId);
        DialogResult = false;
    }

    private static bool IsValidColor(string value)
    {
        try
        {
            _ = (Color)ColorConverter.ConvertFromString(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static SolidColorBrush ThemeBrush(string key) =>
        Application.Current.Resources[key] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Transparent);
}
