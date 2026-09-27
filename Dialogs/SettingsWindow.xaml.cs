using System.Windows;
using System.Windows.Media;
using Chat.Models;

namespace Chat.Dialogs;

public partial class SettingsWindow : Window
{
    private readonly ChatConversation _conversation;

    public SettingsWindow(ChatConversation conversation)
    {
        InitializeComponent();
        _conversation = conversation;

        TimestampCheckBox.IsChecked = conversation.ShowTimestamps;
        TypingCheckBox.IsChecked = conversation.ShowTypingIndicators;
        ReadReceiptsCheckBox.IsChecked = conversation.ShowReadReceipts;
        OutgoingColorBox.Text = conversation.OutgoingBubbleColor;
        IncomingColorBox.Text = conversation.IncomingBubbleColor;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!IsValidColor(OutgoingColorBox.Text.Trim()) || !IsValidColor(IncomingColorBox.Text.Trim()))
        {
            MessageBox.Show(this, "Enter valid hex colors such as #0A84FF.", "Conversation Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _conversation.ShowTimestamps = TimestampCheckBox.IsChecked == true;
        _conversation.ShowTypingIndicators = TypingCheckBox.IsChecked == true;
        _conversation.ShowReadReceipts = ReadReceiptsCheckBox.IsChecked == true;
        _conversation.OutgoingBubbleColor = OutgoingColorBox.Text.Trim();
        _conversation.IncomingBubbleColor = IncomingColorBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

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
}