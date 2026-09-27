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
        UpdateComposerState();
    }

    private void RenderAll()
    {
        RenderConversationList();
        RenderContactsList();
        RenderCurrentConversation();
    }

    private void SelectInitialConversation()
    {
        if (string.IsNullOrWhiteSpace(_project.ActiveConversationId))
            return;

        var ordered = _project.Conversations
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var index = ordered.FindIndex(c => c.Id == _project.ActiveConversationId);

        if (index >= 0)
            ConversationList.SelectedIndex = index;
    }

    private void RenderConversationList()
    {
        ConversationList.Items.Clear();

        var query = SearchBox?.Text?.Trim() ?? "";

        var conversations = _project.Conversations
            .OrderByDescending(c => c.CreatedAt)
            .Where(c => MatchesSearch(c, query));

        foreach (var conversation in conversations)
        {
            var item = new ListBoxItem
            {
                Tag = conversation.Id,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 0, 0, 3),
                Background = Brushes.Transparent
            };

            var wrapper = new Border
            {
                Background = _selectedConversation?.Id == conversation.Id
                    ? new SolidColorBrush(Color.FromRgb(235, 245, 255))
                    : Brushes.Transparent,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var avatar = CreateConversationAvatar(conversation);

            var content = new StackPanel
            {
                Margin = new Thickness(11, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            content.Children.Add(new TextBlock
            {
                Text = conversation.Title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var participantNames = GetParticipants(conversation)
                .Select(c => c.Name)
                .Take(2)
                .ToList();

            content.Children.Add(new TextBlock
            {
                Text = participantNames.Count == 0
                    ? "No participants"
                    : string.Join(", ", participantNames),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
                Margin = new Thickness(0, 3, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var lastMessage = conversation.Messages.LastOrDefault();

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(content, 1);
            grid.Children.Add(avatar);
            grid.Children.Add(content);

            if (lastMessage is not null)
            {
                var time = new TextBlock
                {
                    Text = lastMessage.Timestamp.ToString("HH:mm"),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(152, 162, 179)),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                Grid.SetColumn(time, 2);
                grid.Children.Add(time);
            }

            wrapper.Child = grid;
            item.Content = wrapper;
            ConversationList.Items.Add(item);
        }

        SidebarStatusText.Text = _project.Conversations.Count == 0
            ? "No chats yet"
            : $"{_project.Conversations.Count} practice chat{(_project.Conversations.Count == 1 ? "" : "s")}";
    }

    private void RenderContactsList()
    {
        ContactsList.Items.Clear();

        var query = SearchBox?.Text?.Trim() ?? "";

        var contacts = _project.Contacts
            .Where(c => MatchesSearch(c, query));

        ContactsEmptyHint.Visibility =
            _project.Contacts.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        foreach (var contact in contacts)
        {
            var avatar = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(21),
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

            var text = new StackPanel
            {
                Margin = new Thickness(11, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            text.Children.Add(new TextBlock
            {
                Text = contact.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            });

            text.Children.Add(new TextBlock
            {
                Text = contact.IsAi ? $"{contact.Role} • AI" : contact.Role,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
                Margin = new Thickness(0, 3, 0, 0)
            });

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(text, 1);
            row.Children.Add(avatar);
            row.Children.Add(text);

            ContactsList.Items.Add(new ListBoxItem
            {
                Content = new Border
                {
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10),
                    Child = row
                },
                Tag = contact.Id,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 0, 0, 3)
            });
        }
    }

    private void RenderCurrentConversation()
    {
        MessagesPanel.Children.Clear();

        if (_selectedConversation is null)
        {
            ConversationTitleText.Text = "Chat";
            ConversationSubtitleText.Text = _project.Conversations.Count == 0
                ? "Create a contact, then your first conversation."
                : "Select a conversation from the left.";

            CurrentAvatarText.Text = "?";
            CurrentAvatar.Background = new SolidColorBrush(Color.FromRgb(221, 245, 232));
            CurrentAvatarText.Foreground = new SolidColorBrush(Color.FromRgb(21, 148, 71));

            AiButton.Visibility = Visibility.Collapsed;
            CallButton.IsEnabled = false;
            MoreButton.IsEnabled = false;

            MessagesPanel.Children.Add(CreateEmptyState(
                "Create your first conversation",
                _project.Contacts.Count == 0
                    ? "Start by creating a contact. Nothing is pre-filled."
                    : "Choose New Chat, select a contact, and start writing both sides."));

            UpdateComposerState();
            return;
        }

        var participants = GetParticipants(_selectedConversation);
        var first = participants.FirstOrDefault();

        ConversationTitleText.Text = _selectedConversation.Title;
        ConversationSubtitleText.Text = first?.Name ?? "Practice conversation";

        CurrentAvatarText.Text = _selectedConversation.IsGroup
            ? "＋"
            : first?.Initial ?? "?";

        CurrentAvatar.Background = first is null
            ? new SolidColorBrush(Color.FromRgb(10, 132, 255))
            : ParseBrush(first.AvatarColor);

        CurrentAvatarText.Foreground = Brushes.White;

        AiButton.Visibility = Visibility.Visible;
        AiButton.Content = _selectedConversation.IsAiEnabled ? "AI On" : "AI";
        CallButton.IsEnabled = first is not null;
        MoreButton.IsEnabled = true;

        RenderSendAsCombo();
        RenderMessages();
        UpdateComposerState();
    }

    private FrameworkElement CreateEmptyState(string title, string description)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 460,
            Margin = new Thickness(0, 110, 0, 110)
        };

        panel.Children.Add(new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(32),
            Background = new SolidColorBrush(Color.FromRgb(221, 245, 232)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = "✦",
                Foreground = new SolidColorBrush(Color.FromRgb(21, 148, 71)),
                FontSize = 29,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 0)
        });

        panel.Children.Add(new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(102, 112, 133)),
            Margin = new Thickness(0, 7, 0, 0)
        });

        return panel;
    }

    private void RenderSendAsCombo()
    {
        SendAsCombo.Items.Clear();

        AddSenderOption("self", _project.CurrentUserName);

        foreach (var participant in GetParticipants(_selectedConversation!))
            AddSenderOption(participant.Id, participant.Name);

        var preferred = _selectedConversation!.PerspectiveId;
        var index = FindSenderIndex(preferred);

        SendAsCombo.SelectedIndex = index >= 0 ? index : 0;
        _selectedConversation.PerspectiveId = GetSelectedSenderId() ?? "self";
    }

    private void AddSenderOption(string id, string name)
    {
        SendAsCombo.Items.Add(new ComboBoxItem
        {
            Content = id == "self" ? $"{name} (you)" : name,
            Tag = id
        });
    }

    private int FindSenderIndex(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return -1;

        for (var i = 0; i < SendAsCombo.Items.Count; i++)
        {
            if (SendAsCombo.Items[i] is ComboBoxItem item &&
                item.Tag?.ToString() == id)
            {
                return i;
            }
        }

        return -1;
    }

    private string? GetSelectedSenderId() =>
        (SendAsCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

    private void RenderMessages()
    {
        MessagesPanel.Children.Clear();

        if (_selectedConversation is null)
            return;

        if (_selectedConversation.Messages.Count == 0)
        {
            MessagesPanel.Children.Add(CreateEmptyState(
                "Start the conversation",
                "Type a message below. Use Send as to rehearse both sides."));
            return;
        }

        DateTime? previousDay = null;

        foreach (var message in _selectedConversation.Messages.OrderBy(m => m.Timestamp))
        {
            var day = message.Timestamp.Date;

            if (previousDay is null || previousDay.Value != day)
            {
                MessagesPanel.Children.Add(new TextBlock
                {
                    Text = day == DateTime.Today
                        ? "Today"
                        : message.Timestamp.ToString("MMM d"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(152, 162, 179)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 16)
                });

                previousDay = day;
            }

            MessagesPanel.Children.Add(CreateMessageElement(message));
        }

        Dispatcher.BeginInvoke(
            () => MessagesScrollViewer.ScrollToEnd(),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private FrameworkElement CreateMessageElement(ChatMessage message)
    {
        var sender = message.SenderId == "self"
            ? null
            : GetContact(message.SenderId);

        var isOutgoing = message.SenderId == "self";
        var isSticker = message.Kind == ChatMessageKind.Sticker;

        var wrapper = new StackPanel
        {
            HorizontalAlignment = isOutgoing
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,
            Margin = isOutgoing
                ? new Thickness(120, 0, 0, 10)
                : new Thickness(0, 0, 120, 10)
        };

        if (_selectedConversation!.IsGroup && !isOutgoing && sender is not null)
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

        var bubble = new Border
        {
            Background = isOutgoing
                ? new SolidColorBrush(Color.FromRgb(10, 132, 255))
                : new SolidColorBrush(Color.FromRgb(235, 235, 239)),
            CornerRadius = isOutgoing
                ? new CornerRadius(17, 17, 5, 17)
                : new CornerRadius(17, 17, 17, 5),
            Padding = isSticker
                ? new Thickness(7)
                : new Thickness(14, 10, 14, 10),
            MaxWidth = 580
        };

        bubble.Child = new TextBlock
        {
            Text = message.Text,
            FontSize = isSticker ? 40 : 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = isOutgoing
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(17, 24, 39))
        };

        wrapper.Children.Add(bubble);

        var meta = new List<string>();

        if (_selectedConversation.ShowTimestamps)
            meta.Add(message.Timestamp.ToString("h:mm tt"));

        if (_selectedConversation.ShowReadReceipts && isOutgoing && message.IsRead)
            meta.Add("Read");

        if (message.Reaction is not null)
            meta.Add(message.Reaction);

        if (meta.Count > 0)
        {
            wrapper.Children.Add(new TextBlock
            {
                Text = string.Join("  ", meta),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(152, 162, 179)),
                HorizontalAlignment = isOutgoing
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left,
                Margin = isOutgoing
                    ? new Thickness(0, 4, 7, 0)
                    : new Thickness(7, 4, 0, 0)
            });
        }

        var menu = new ContextMenu();

        var reactMenu = new MenuItem { Header = "React" };
        foreach (var emoji in new[] { "❤️", "👍", "😂", "😮", "🔥" })
        {
            var reactionItem = new MenuItem { Header = emoji };
            reactionItem.Click += (_, _) =>
            {
                message.Reaction = emoji;
                RenderMessages();
                MarkDirty();
            };
            reactMenu.Items.Add(reactionItem);
        }

        menu.Items.Add(reactMenu);

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

        return wrapper;
    }

    private Border CreateConversationAvatar(ChatConversation conversation)
    {
        var first = GetParticipants(conversation).FirstOrDefault();

        var border = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(21),
            Background = first is null
                ? new SolidColorBrush(Color.FromRgb(221, 245, 232))
                : ParseBrush(first.AvatarColor)
        };

        border.Child = new TextBlock
        {
            Text = conversation.IsGroup ? "＋" : first?.Initial ?? "?",
            Foreground = first is null
                ? new SolidColorBrush(Color.FromRgb(21, 148, 71))
                : Brushes.White,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return border;
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
        MarkDirty();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderConversationList();
        RenderContactsList();
    }

    private void SendAsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedConversation is null ||
            SendAsCombo.SelectedItem is not ComboBoxItem item)
            return;

        _selectedConversation.PerspectiveId = item.Tag?.ToString() ?? "self";
        RenderMessages();
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e) =>
        await SendCurrentMessageAsync();

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

        var senderId = _selectedConversation.PerspectiveId;

        if (string.IsNullOrWhiteSpace(senderId))
            senderId = "self";

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
            ConversationSubtitleText.Text = $"{aiContact.Name} is typing…";
            await Task.Delay(850);
        }

        _selectedConversation.Messages.Add(new ChatMessage
        {
            SenderId = aiContact.Id,
            Text = AiResponseService.Generate(aiContact, message),
            Timestamp = DateTime.Now,
            IsRead = true
        });

        RenderCurrentConversation();
        MarkDirty();
    }

    private void Emoji_Click(object sender, RoutedEventArgs e)
    {
        MessageInput.Text += MessageInput.Text.Length == 0
            ? "🙂"
            : " 🙂";

        MessageInput.CaretIndex = MessageInput.Text.Length;
        MessageInput.Focus();
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Contacts.Count == 0)
        {
            var result = MessageBox.Show(
                this,
                "Create a contact first. Nothing is pre-filled in Chat. Create one now?",
                "New Chat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                AddContact_Click(sender, e);

            if (_project.Contacts.Count == 0)
                return;
        }

        var dialog = new CreateConversationWindow(_project.Contacts)
        {
            Owner = this
        };

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

        var ordered = _project.Conversations
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var index = ordered.FindIndex(c => c.Id == conversation.Id);

        if (index >= 0)
            ConversationList.SelectedIndex = index;

        MarkDirty();
    }

    private void AddContact_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateContactWindow
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true || dialog.CreatedContact is null)
            return;

        _project.Contacts.Add(dialog.CreatedContact);

        RenderContactsList();
        ShowChatsTab();
        MarkDirty();
    }

    private void ChatsTab_Click(object sender, RoutedEventArgs e) =>
        ShowChatsTab();

    private void ContactsTab_Click(object sender, RoutedEventArgs e)
    {
        ChatsPanel.Visibility = Visibility.Collapsed;
        ContactsPanel.Visibility = Visibility.Visible;

        ChatsTabButton.Foreground =
            new SolidColorBrush(Color.FromRgb(102, 112, 133));

        ContactsTabButton.Foreground =
            new SolidColorBrush(Color.FromRgb(10, 132, 255));
    }

    private void ShowChatsTab()
    {
        ChatsPanel.Visibility = Visibility.Visible;
        ContactsPanel.Visibility = Visibility.Collapsed;

        ChatsTabButton.Foreground =
            new SolidColorBrush(Color.FromRgb(10, 132, 255));

        ContactsTabButton.Foreground =
            new SolidColorBrush(Color.FromRgb(102, 112, 133));
    }

    private void SidebarSettings_Click(object sender, RoutedEventArgs e)
    {
        Settings_Click(sender, e);
    }

    private void AiPractice_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        _selectedConversation.IsAiEnabled = !_selectedConversation.IsAiEnabled;

        if (_selectedConversation.IsAiEnabled &&
            !GetParticipants(_selectedConversation).Any(c => c.IsAi))
        {
            MessageBox.Show(
                this,
                "AI mode is enabled, but no participant is marked as an AI persona.",
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

        var dialog = new SettingsWindow(_selectedConversation)
        {
            Owner = this
        };

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

        var dialog = new CallWindow(_selectedConversation, contact)
        {
            Owner = this
        };

        dialog.ShowDialog();
    }

    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = MoreButton,
            StaysOpen = false
        };

        var customize = new MenuItem { Header = "Customize chat" };
        customize.Click += Settings_Click;
        menu.Items.Add(customize);

        var export = new MenuItem { Header = "Export as PNG" };
        export.Click += Export_Click;
        menu.Items.Add(export);

        menu.Items.Add(new Separator());

        var delete = new MenuItem
        {
            Header = "Delete conversation",
            Foreground = Brushes.IndianRed
        };
        delete.Click += DeleteConversation_Click;
        menu.Items.Add(delete);

        menu.IsOpen = true;
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
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not export the conversation.\n\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

    private static bool MatchesSearch(ChatConversation conversation, string query) =>
        string.IsNullOrWhiteSpace(query) ||
        conversation.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        conversation.Scenario.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesSearch(ChatCharacter contact, string query) =>
        string.IsNullOrWhiteSpace(query) ||
        contact.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        contact.Role.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        contact.Context.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void MarkDirty()
    {
        try
        {
            _storage.Save(_project);
        }
        catch
        {
            // Persistence failure is non-fatal; the current session remains usable.
        }
    }

    private void UpdateComposerState()
    {
        var enabled = _selectedConversation is not null;

        MessageInput.IsEnabled = enabled;
        SendButton.IsEnabled = enabled;
        EmojiButton.IsEnabled = enabled;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(value)
            ? "conversation"
            : value;
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