using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chat.Dialogs;
using Chat.Models;
using Chat.Services;
using Microsoft.Win32;

namespace Chat;

public partial class MainWindow : Window
{
    private readonly ProjectStorageService _storage = new();
    private ChatProject _project;
    private ChatConversation? _selectedConversation;

    public MainWindow()
    {
        InitializeComponent();
        _project = _storage.Load();
        RenderAll();
        SelectInitialConversation();
    }

    private void RenderAll()
    {
        RenderConversationList();
        RenderContactsList();
        RenderCurrentConversation();
    }

    private void SelectInitialConversation()
    {
        if (_project.ActiveConversationId is not null)
        {
            var ordered = _project.Conversations.OrderByDescending(c => c.CreatedAt).ToList();
            var index = ordered.FindIndex(c => c.Id == _project.ActiveConversationId);
            if (index >= 0)
                ConversationList.SelectedIndex = index;
        }

        if (_selectedConversation is null && _project.Conversations.Count == 1)
            ConversationList.SelectedIndex = 0;
    }

    private void RenderConversationList()
    {
        ConversationList.Items.Clear();

        foreach (var conversation in _project.Conversations.OrderByDescending(c => c.CreatedAt))
        {
            var lastMessage = conversation.Messages.LastOrDefault();
            var preview = lastMessage is null
                ? "No messages yet"
                : lastMessage.Kind == ChatMessageKind.Sticker
                    ? "Sticker"
                    : lastMessage.Text.Replace(Environment.NewLine, " ").Trim();

            if (preview.Length > 40)
                preview = preview[..40] + "…";

            var item = new ListBoxItem
            {
                Tag = conversation.Id,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 0, 0, 5),
                Background = Brushes.Transparent
            };

            var wrapper = new Border
            {
                Background = _selectedConversation?.Id == conversation.Id
                    ? new SolidColorBrush(Color.FromRgb(236, 245, 255))
                    : Brushes.Transparent,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatar = CreateConversationAvatar(conversation);
            var text = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            text.Children.Add(new TextBlock
            {
                Text = conversation.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            text.Children.Add(new TextBlock
            {
                Text = preview,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
                Margin = new Thickness(0, 3, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(text, 1);
            grid.Children.Add(avatar);
            grid.Children.Add(text);
            wrapper.Child = grid;
            item.Content = wrapper;

            ConversationList.Items.Add(item);
        }
    }

    private void RenderContactsList()
    {
        ContactsList.Items.Clear();

        foreach (var contact in _project.Contacts)
        {
            var border = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatar = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = ParseBrush(contact.AvatarColor)
            };
            avatar.Child = new TextBlock
            {
                Text = contact.Initial,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var text = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = contact.Name, FontWeight = FontWeights.SemiBold, FontSize = 13 });
            text.Children.Add(new TextBlock
            {
                Text = contact.IsAi ? $"{contact.Role} • AI" : contact.Role,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
                Margin = new Thickness(0, 3, 0, 0)
            });

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(text, 1);
            grid.Children.Add(avatar);
            grid.Children.Add(text);
            border.Child = grid;

            ContactsList.Items.Add(new ListBoxItem
            {
                Content = border,
                Tag = contact.Id,
                Padding = new Thickness(4)
            });
        }
    }

    private void RenderCurrentConversation()
    {
        MessagesPanel.Children.Clear();

        if (_selectedConversation is null)
        {
            ConversationTitleText.Text = "No conversation selected";
            ConversationSubtitleText.Text = _project.Conversations.Count == 0
                ? "Create a contact and then your first chat."
                : "Select a conversation from the left.";
            EmptyDetailsCard.Visibility = Visibility.Visible;
            ConversationDetailsPanel.Visibility = Visibility.Collapsed;
            SendAsCombo.Items.Clear();
            MessageInput.IsEnabled = false;
            return;
        }

        MessageInput.IsEnabled = true;
        ConversationTitleText.Text = _selectedConversation.Title;

        var participants = GetParticipants(_selectedConversation);
        var subtitleNames = participants.Select(p => p.Name).ToList();
        ConversationSubtitleText.Text = _selectedConversation.IsGroup
            ? $"{subtitleNames.Count} participants • Group practice"
            : subtitleNames.Count == 1
                ? subtitleNames[0]
                : "Practice conversation";

        EmptyDetailsCard.Visibility = Visibility.Collapsed;
        ConversationDetailsPanel.Visibility = Visibility.Visible;
        ScenarioText.Text = string.IsNullOrWhiteSpace(_selectedConversation.Scenario)
            ? "No scenario text. You control the conversation."
            : _selectedConversation.Scenario;

        ParticipantList.Items.Clear();
        foreach (var participant in participants)
        {
            ParticipantList.Items.Add(new TextBlock
            {
                Text = participant.IsAi
                    ? $"{participant.Name}  •  {participant.Role}  •  AI"
                    : $"{participant.Name}  •  {participant.Role}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        PracticeModeText.Text = _selectedConversation.IsAiEnabled
            ? "AI conversation mode is on. Send a message to trigger a local simulated response."
            : "Manual practice. Switch Send as to rehearse either side.";

        AiPracticeButton.Content = _selectedConversation.IsAiEnabled
            ? "Disable AI Practice"
            : "Enable AI Practice";

        RenderSendAsCombo();
        RenderMessages();
    }

    private void RenderSendAsCombo()
    {
        var previousId = GetSelectedSenderId();
        SendAsCombo.Items.Clear();

        AddSenderOption("self", _project.CurrentUserName);

        foreach (var participant in GetParticipants(_selectedConversation!))
            AddSenderOption(participant.Id, participant.Name);

        var index = FindSenderIndex(previousId);
        SendAsCombo.SelectedIndex = index >= 0 ? index : 0;
        _selectedConversation!.PerspectiveId = GetSelectedSenderId() ?? "self";
    }

    private void AddSenderOption(string id, string name)
    {
        SendAsCombo.Items.Add(new ComboBoxItem
        {
            Content = id == "self" ? $"{name}  (you)" : name,
            Tag = id
        });
    }

    private int FindSenderIndex(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return -1;

        for (var i = 0; i < SendAsCombo.Items.Count; i++)
        {
            if (SendAsCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == id)
                return i;
        }

        return -1;
    }

    private string? GetSelectedSenderId() => (SendAsCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

    private void RenderMessages()
    {
        MessagesPanel.Children.Clear();

        if (_selectedConversation is null)
            return;

        if (_selectedConversation.Messages.Count == 0)
        {
            var empty = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 420,
                Margin = new Thickness(0, 80, 0, 80)
            };

            empty.Children.Add(new Border
            {
                Width = 54,
                Height = 54,
                CornerRadius = new CornerRadius(27),
                Background = new SolidColorBrush(Color.FromRgb(226, 240, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "✦",
                    Foreground = new SolidColorBrush(Color.FromRgb(10, 132, 255)),
                    FontSize = 26,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            empty.Children.Add(new TextBlock
            {
                Text = "Start the conversation",
                FontSize = 19,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 13, 0, 0)
            });

            empty.Children.Add(new TextBlock
            {
                Text = "Write both sides, switch participants, react to messages, or turn on AI practice.",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
                Margin = new Thickness(0, 7, 0, 0)
            });

            MessagesPanel.Children.Add(empty);
            return;
        }

        foreach (var message in _selectedConversation.Messages)
            MessagesPanel.Children.Add(CreateMessageElement(message));

        Dispatcher.BeginInvoke(() => MessagesScrollViewer.ScrollToEnd(), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private FrameworkElement CreateMessageElement(ChatMessage message)
    {
        var sender = message.SenderId == "self" ? null : GetContact(message.SenderId);
        var isPerspective = message.SenderId == _selectedConversation!.PerspectiveId;
        var isSticker = message.Kind == ChatMessageKind.Sticker;

        var wrapper = new StackPanel
        {
            HorizontalAlignment = isPerspective ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Margin = isPerspective
                ? new Thickness(100, 0, 0, 11)
                : new Thickness(0, 0, 100, 11),
            Cursor = Cursors.Hand
        };

        if (_selectedConversation.IsGroup && !isPerspective && sender is not null)
        {
            wrapper.Children.Add(new TextBlock
            {
                Text = sender.Name,
                FontSize = 10,
                Foreground = ParseBrush(sender.AvatarColor),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(7, 0, 0, 4)
            });
        }

        var bubbleColor = isPerspective
            ? _selectedConversation.OutgoingBubbleColor
            : _selectedConversation.IncomingBubbleColor;

        var bubble = new Border
        {
            Background = ParseBrush(bubbleColor),
            CornerRadius = isPerspective
                ? new CornerRadius(18, 18, 5, 18)
                : new CornerRadius(18, 18, 18, 5),
            Padding = isSticker ? new Thickness(7) : new Thickness(13, 10, 13, 10),
            MaxWidth = 610
        };

        bubble.Child = new TextBlock
        {
            Text = message.Text,
            FontSize = isSticker ? 42 : 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = isPerspective ? Brushes.White : new SolidColorBrush(Color.FromRgb(17, 24, 39))
        };

        wrapper.Children.Add(bubble);

        if (_selectedConversation.ShowTimestamps || _selectedConversation.ShowReadReceipts || message.Reaction is not null)
        {
            var metaParts = new List<string>();

            if (_selectedConversation.ShowTimestamps)
                metaParts.Add(message.Timestamp.ToString("HH:mm"));

            if (_selectedConversation.ShowReadReceipts && isPerspective && message.IsRead)
                metaParts.Add("✓✓");

            if (message.Reaction is not null)
                metaParts.Add(message.Reaction);

            if (metaParts.Count > 0)
            {
                wrapper.Children.Add(new TextBlock
                {
                    Text = string.Join("  ", metaParts),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
                    HorizontalAlignment = isPerspective ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Margin = isPerspective ? new Thickness(0, 4, 7, 0) : new Thickness(7, 4, 0, 0)
                });
            }
        }

        var menu = new ContextMenu();
        var react = new MenuItem { Header = "React" };

        foreach (var emoji in new[] { "❤️", "👍", "😂", "😮", "🔥" })
        {
            var reactionItem = new MenuItem { Header = emoji };
            reactionItem.Click += (_, _) =>
            {
                message.Reaction = emoji;
                RenderMessages();
                MarkDirty();
            };
            react.Items.Add(reactionItem);
        }

        menu.Items.Add(react);

        var copy = new MenuItem { Header = "Copy" };
        copy.Click += (_, _) => Clipboard.SetText(message.Text);
        menu.Items.Add(copy);

        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) =>
        {
            _selectedConversation.Messages.Remove(message);
            RenderCurrentConversation();
            MarkDirty();
        };
        menu.Items.Add(delete);

        wrapper.ContextMenu = menu;

        wrapper.MouseLeftButtonUp += (_, _) =>
        {
            message.Reaction = message.Reaction is null ? "❤️" : null;
            RenderMessages();
            MarkDirty();
        };

        return wrapper;
    }

    private Border CreateConversationAvatar(ChatConversation conversation)
    {
        var first = GetParticipants(conversation).FirstOrDefault();

        var avatar = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(19),
            Background = first is null
                ? new SolidColorBrush(Color.FromRgb(10, 132, 255))
                : ParseBrush(first.AvatarColor)
        };

        avatar.Child = new TextBlock
        {
            Text = conversation.IsGroup ? "＋" : first?.Initial ?? "?",
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return avatar;
    }

    private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationList.SelectedItem is not ListBoxItem item)
            return;

        var id = item.Tag?.ToString();
        _selectedConversation = _project.Conversations.FirstOrDefault(c => c.Id == id);
        _project.ActiveConversationId = _selectedConversation?.Id;

        RenderCurrentConversation();
        RenderConversationList();
    }

    private void SendAsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedConversation is null || SendAsCombo.SelectedItem is not ComboBoxItem item)
            return;

        _selectedConversation.PerspectiveId = item.Tag?.ToString() ?? "self";
        GlobalStatusText.Text = $"Practicing as {item.Content}";
        RenderMessages();
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e) => await SendCurrentMessageAsync();

    private async void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            await SendCurrentMessageAsync();
        }
    }

    private async Task SendCurrentMessageAsync()
    {
        if (_selectedConversation is null)
            return;

        var text = MessageInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var senderId = GetSelectedSenderId() ?? "self";

        var message = new ChatMessage
        {
            SenderId = senderId,
            Text = text,
            Timestamp = DateTime.Now,
            IsRead = _selectedConversation.ShowReadReceipts
        };

        _selectedConversation.Messages.Add(message);
        MessageInput.Clear();

        RenderCurrentConversation();
        MarkDirty();

        if (!_selectedConversation.IsAiEnabled)
            return;

        var aiContact = GetParticipants(_selectedConversation)
            .FirstOrDefault(c => c.IsAi && c.Id != senderId);

        if (aiContact is null)
            return;

        if (_selectedConversation.ShowTypingIndicators)
        {
            GlobalStatusText.Text = $"{aiContact.Name} is typing…";
            await Task.Delay(850);
        }

        _selectedConversation.Messages.Add(new ChatMessage
        {
            SenderId = aiContact.Id,
            Text = AiResponseService.Generate(aiContact, message),
            Timestamp = DateTime.Now,
            IsRead = true
        });

        GlobalStatusText.Text = "AI reply added";
        RenderCurrentConversation();
        MarkDirty();
    }

    private void Emoji_Click(object sender, RoutedEventArgs e)
    {
        MessageInput.Text += MessageInput.Text.Length == 0 ? "🙂" : " 🙂";
        MessageInput.CaretIndex = MessageInput.Text.Length;
        MessageInput.Focus();
    }

    private void Sticker_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var senderId = GetSelectedSenderId() ?? "self";

        _selectedConversation.Messages.Add(new ChatMessage
        {
            SenderId = senderId,
            Text = "❤️",
            Timestamp = DateTime.Now,
            Kind = ChatMessageKind.Sticker,
            IsRead = true
        });

        RenderCurrentConversation();
        MarkDirty();
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Contacts.Count == 0)
        {
            var result = MessageBox.Show(
                this,
                "You need at least one contact before creating a conversation. Create one now?",
                "New Conversation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                AddContact_Click(sender, e);

            if (_project.Contacts.Count == 0)
                return;
        }

        var dialog = new CreateConversationWindow(_project.Contacts) { Owner = this };

        if (dialog.ShowDialog() != true)
            return;

        var conversation = new ChatConversation
        {
            Title = dialog.ConversationTitle,
            Scenario = dialog.Scenario,
            ParticipantIds = dialog.ParticipantIds,
            IsAiEnabled = dialog.AiEnabled,
            PerspectiveId = "self"
        };

        _project.Conversations.Insert(0, conversation);
        _project.ActiveConversationId = conversation.Id;
        _selectedConversation = conversation;

        RenderAll();
        var ordered = _project.Conversations.OrderByDescending(c => c.CreatedAt).ToList();
        ConversationList.SelectedIndex = ordered.FindIndex(c => c.Id == conversation.Id);
        MarkDirty();
    }

    private void AddContact_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateContactWindow { Owner = this };

        if (dialog.ShowDialog() != true || dialog.CreatedContact is null)
            return;

        _project.Contacts.Add(dialog.CreatedContact);
        RenderContactsList();
        ShowChatsTab();
        MarkDirty();
    }

    private void ChatsTab_Click(object sender, RoutedEventArgs e) => ShowChatsTab();

    private void ContactsTab_Click(object sender, RoutedEventArgs e)
    {
        ChatsPanel.Visibility = Visibility.Collapsed;
        ContactsPanel.Visibility = Visibility.Visible;
        ChatsTabButton.Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133));
        ContactsTabButton.Foreground = new SolidColorBrush(Color.FromRgb(10, 132, 255));
    }

    private void ShowChatsTab()
    {
        ChatsPanel.Visibility = Visibility.Visible;
        ContactsPanel.Visibility = Visibility.Collapsed;
        ChatsTabButton.Foreground = new SolidColorBrush(Color.FromRgb(10, 132, 255));
        ContactsTabButton.Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133));
    }

    private void AiPractice_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        _selectedConversation.IsAiEnabled = !_selectedConversation.IsAiEnabled;

        var hasAi = GetParticipants(_selectedConversation).Any(c => c.IsAi);

        if (_selectedConversation.IsAiEnabled && !hasAi)
        {
            MessageBox.Show(
                this,
                "AI practice is enabled, but no participant is marked as an AI persona. Create an AI contact first.",
                "AI Practice",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        RenderCurrentConversation();
        MarkDirty();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var dialog = new SettingsWindow(_selectedConversation) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            RenderCurrentConversation();
            MarkDirty();
        }
    }

    private void Call_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var contact = GetParticipants(_selectedConversation).FirstOrDefault();
        if (contact is null)
            return;

        var dialog = new CallWindow(_selectedConversation, contact) { Owner = this };
        dialog.ShowDialog();
    }

    private void DeleteConversation_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var result = MessageBox.Show(
            this,
            $"Delete “{_selectedConversation.Title}”?",
            "Delete Conversation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        _project.Conversations.Remove(_selectedConversation);
        _selectedConversation = null;
        _project.ActiveConversationId = null;

        RenderAll();
        MarkDirty();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _storage.Save(_project);
            SavedText.Text = "Saved";
            GlobalStatusText.Text = "Saved locally";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save the project.\n\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "PNG image|*.png",
            FileName = $"{SanitizeFileName(_selectedConversation.Title)}.png"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            ChatImageExportService.Export(ExportSurface, dialog.FileName);
            GlobalStatusText.Text = "Conversation exported";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not export the conversation.\n\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private IReadOnlyList<ChatCharacter> GetParticipants(ChatConversation conversation) =>
        conversation.ParticipantIds
            .Select(GetContact)
            .Where(c => c is not null)
            .Cast<ChatCharacter>()
            .ToList();

    private ChatCharacter? GetContact(string id) =>
        _project.Contacts.FirstOrDefault(c => c.Id == id);

    private void MarkDirty()
    {
        SavedText.Text = "Unsaved changes";
        GlobalStatusText.Text = "Changes pending save";
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(value) ? "conversation" : value;
    }

    private static SolidColorBrush ParseBrush(string color)
    {
        try
        {
            var parsed = (Color)ColorConverter.ConvertFromString(color);
            return new SolidColorBrush(parsed);
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(10, 132, 255));
        }
    }
}