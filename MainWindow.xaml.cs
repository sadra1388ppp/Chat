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
    private bool _refreshingConversationList;
    private ChatMessage? _activeMessageAction;


    public MainWindow()
    {
        InitializeComponent();
        _project = _storage.Load();
        ThemeService.ApplyTheme(_project.ThemeId);
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
            .OrderByDescending(c => c.Messages.LastOrDefault()?.Timestamp ?? c.CreatedAt)
            .Where(c => MatchesSearch(c, query))
            .ToList();

        if (conversations.Count == 0)
        {
            ConversationList.Items.Add(new ListBoxItem
            {
                IsHitTestVisible = false,
                Content = new Border
                {
                    Background = GetThemeBrush("SoftPanel"),
                    BorderBrush = GetThemeBrush("Divider"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(18),
                    Margin = new Thickness(4, 4, 4, 0),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "No conversations yet",
                                FontSize = 14,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = GetThemeBrush("TextPrimary")
                            },
                            new TextBlock
                            {
                                Text = _project.Contacts.Count == 0
                                    ? "Create a contact first, then start your first chat."
                                    : "Create a conversation with the + button above.",
                                FontSize = 11,
                                Foreground = GetThemeBrush("TextSecondary"),
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 6, 0, 0)
                            }
                        }
                    }
                }
            });

            SidebarStatusText.Text = "No conversations yet";
            return;
        }

        foreach (var conversation in conversations)
        {
            var item = new ListBoxItem
            {
                Tag = conversation.Id,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 1, 0, 1),
                Background = Brushes.Transparent
            };

            var wrapper = new Border
            {
                Background = _selectedConversation?.Id == conversation.Id
                    ? GetThemeBrush("SelectedBackground")
                    : Brushes.Transparent,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(11, 10, 10, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var avatar = CreateConversationAvatar(conversation);
            avatar.Width = 48;
            avatar.Height = 48;
            avatar.CornerRadius = new CornerRadius(24);

            var content = new Grid
            {
                Margin = new Thickness(12, 1, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var participantNames = GetParticipants(conversation)
                .Select(c => c.Name)
                .Take(2)
                .ToList();

            var title = new TextBlock
            {
                Text = conversation.Title,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var lastMessage = conversation.Messages
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault();

            var preview = lastMessage is null
                ? (string.IsNullOrWhiteSpace(conversation.Scenario)
                    ? (participantNames.Count == 0 ? "New conversation" : string.Join(", ", participantNames))
                    : conversation.Scenario)
                : (lastMessage.SenderId == "self"
                    ? $"You: {lastMessage.Text}"
                    : lastMessage.Text);

            if (preview.Length > 46)
                preview = preview[..46] + "…";

            var previewText = new TextBlock
            {
                Text = preview,
                FontSize = 11,
                Foreground = GetThemeBrush("TextSecondary"),
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            Grid.SetRow(title, 0);
            Grid.SetRow(previewText, 1);
            content.Children.Add(title);
            content.Children.Add(previewText);

            var time = new TextBlock
            {
                Text = (lastMessage?.Timestamp ?? conversation.CreatedAt).ToString("HH:mm"),
                FontSize = 9.5,
                Foreground = GetThemeBrush("TextSecondary"),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 1, 0)
            };

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(content, 1);
            Grid.SetColumn(time, 2);

            grid.Children.Add(avatar);
            grid.Children.Add(content);
            grid.Children.Add(time);

            wrapper.Child = grid;
            item.Content = wrapper;

            var deleteMenuItem = new MenuItem
            {
                Header = "Delete conversation",
                Style = (Style)FindResource("ConversationContextMenuItemStyle")
            };
            deleteMenuItem.Click += ConversationContextDelete_Click;

            item.ContextMenu = new ContextMenu
            {
                Style = (Style)FindResource("ConversationContextMenuStyle"),
                Items = { deleteMenuItem }
            };

            item.PreviewMouseRightButtonDown += ConversationListItem_RightClick;

            ConversationList.Items.Add(item);
        }

        SidebarStatusText.Text = $"{_project.Conversations.Count} conversation{(_project.Conversations.Count == 1 ? "" : "s")}";
    }

    private void ConversationListItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item)
            return;

        ConversationList.SelectedItem = item;
    }

    private void ConversationContextDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem ||
            menuItem.Parent is not ContextMenu contextMenu ||
            contextMenu.PlacementTarget is not ListBoxItem item ||
            item.Tag is not string conversationId)
            return;

        var conversation = _project.Conversations
            .FirstOrDefault(c => c.Id == conversationId);

        if (conversation is null)
            return;

        var result = MessageBox.Show(
            this,
            $"Delete “{conversation.Title}”?\n\nThis conversation and all of its messages will be removed from this device.",
            "Delete Conversation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        _project.Conversations.Remove(conversation);

        if (_selectedConversation?.Id == conversationId)
        {
            _selectedConversation = null;
            _project.ActiveConversationId = null;
        }

        RenderAll();
        MarkDirty();
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
            var avatar = CreateContactAvatar(contact, 46);

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
                Text = contact.Role,
                FontSize = 11,
                Foreground = GetThemeBrush("TextSecondary"),
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

            UpdateHeaderAvatar(null, false);

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
        ConversationSubtitleText.Text = _selectedConversation.IsGroup
            ? $"{participants.Count} participants"
            : first?.Name ?? "Practice conversation";

        UpdateHeaderAvatar(first, _selectedConversation.IsGroup);

        CallButton.IsEnabled = first is not null && !_selectedConversation.IsGroup;
        MoreButton.IsEnabled = true;

        RenderSendAsIndicator();
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

    private string GetSenderPaletteColor(string senderId)
    {
        if (_selectedConversation is null)
            return ThemeService.GetIncomingBubbleColor(_project.ThemeId);

        var index = _selectedConversation.ParticipantIds.FindIndex(id => id == senderId);

        return index >= 0
            ? ThemeService.GetParticipantBubbleColor(_project.ThemeId, index)
            : ThemeService.GetIncomingBubbleColor(_project.ThemeId);
    }

    private void RenderSendAsIndicator()
    {
        if (_selectedConversation is null)
            return;

        var options = new List<(string Id, string Name)>
        {
            ("self", _project.CurrentUserName)
        };

        options.AddRange(
            GetParticipants(_selectedConversation)
                .Select(c => (c.Id, c.Name)));

        if (options.Count == 0)
            return;

        var currentIndex = options.FindIndex(
            option => option.Id == _selectedConversation.PerspectiveId);

        if (currentIndex < 0)
            currentIndex = 0;

        _selectedConversation.PerspectiveId = options[currentIndex].Id;

        var currentId = _selectedConversation.PerspectiveId;

        // Keep normal chats instantly understandable:
        // self = neutral gray, other participant = blue.
        // Groups use the participant palette below.
        var indicatorColor = _selectedConversation.IsGroup
            ? (currentId == "self"
                ? "#AEB4BE"
                : GetSenderPaletteColor(currentId))
            : (currentId == "self"
                ? "#AEB4BE"
                : "#0A84FF");

        SendAsIndicator.Fill = ParseBrush(indicatorColor);
        SendAsButton.ToolTip = $"Send as {options[currentIndex].Name}";
    }

    private void SendAsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedConversation is null)
            return;

        var options = new List<(string Id, string Name)>
        {
            ("self", _project.CurrentUserName)
        };

        options.AddRange(
            GetParticipants(_selectedConversation)
                .Select(c => (c.Id, c.Name)));

        if (options.Count < 2)
            return;

        var currentIndex = options.FindIndex(
            option => option.Id == _selectedConversation.PerspectiveId);

        if (currentIndex < 0)
            currentIndex = 0;

        var nextIndex = (currentIndex + 1) % options.Count;
        _selectedConversation.PerspectiveId = options[nextIndex].Id;

        RenderSendAsIndicator();
        MarkDirty();
    }

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
                    Foreground = GetThemeBrush("TextSecondary"),
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
        var bubbleColor = GetMessageBubbleColor(message.SenderId);

        var wrapper = new StackPanel
        {
            HorizontalAlignment = isOutgoing
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,
            Margin = isOutgoing
                ? new Thickness(0, 0, 18, 10)
                : new Thickness(18, 0, 0, 10)
        };

        if (_selectedConversation!.IsGroup && !isOutgoing && sender is not null)
        {
            wrapper.Children.Add(new TextBlock
            {
                Text = sender.Name,
                FontSize = 10,
                Foreground = ParseBrush(GetSenderPaletteColor(sender.Id)),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(7, 0, 0, 4)
            });
        }

        var bubble = new Border
        {
            Background = ParseBrush(bubbleColor),
            CornerRadius = isOutgoing
                ? new CornerRadius(17, 17, 5, 17)
                : new CornerRadius(17, 17, 17, 5),
            Padding = new Thickness(14, 10, 14, 10),
            MaxWidth = 620,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 1,
                Opacity = 0.10
            }
        };

        bubble.Child = new TextBlock
        {
            Text = message.Text,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = IsLightColor(bubbleColor)
                ? new SolidColorBrush(Color.FromRgb(17, 24, 39))
                : Brushes.White
        };

        var messageRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = isOutgoing
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left
        };

        messageRow.Children.Add(bubble);

        wrapper.PreviewMouseRightButtonUp += (_, e) =>
        {
            e.Handled = true;
            ShowMessageActionsFlyout(message, e.GetPosition(OverlayCanvas));
        };

        wrapper.Children.Add(messageRow);

        if (!string.IsNullOrWhiteSpace(message.Reaction))
        {
            var reactionPill = new Border
            {
                Background = GetThemeBrush("AccentSoft"),
                BorderBrush = GetThemeBrush("Divider"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7, 3, 8, 3),
                HorizontalAlignment = isOutgoing
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left,
                Margin = isOutgoing
                    ? new Thickness(0, 5, 40, 0)
                    : new Thickness(40, 5, 0, 0)
            };

            var reactionRow = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            reactionRow.Children.Add(new TextBlock
            {
                Text = message.Reaction,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center
            });

            reactionRow.Children.Add(new TextBlock
            {
                Text = " 1",
                FontSize = 9,
                Foreground = GetThemeBrush("TextSecondary"),
                VerticalAlignment = VerticalAlignment.Center
            });

            reactionPill.Child = reactionRow;
            wrapper.Children.Add(reactionPill);
        }

        var meta = new List<string>();

        if (_selectedConversation.ShowTimestamps)
            meta.Add(message.Timestamp.ToString("h:mm tt"));

        if (_selectedConversation.ShowReadReceipts && isOutgoing && message.IsRead)
            meta.Add("Read");

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

        return wrapper;
    }

    private void ShowMessageActionsFlyout(ChatMessage message, Point position)
    {
        _activeMessageAction = message;
        CloseConversationActionsFlyout();
        ReactionActionsFlyout.Visibility = Visibility.Collapsed;

        MessageActionsHeader.Text = "MESSAGE";
        MessageReactFlyoutLabel.Text = string.IsNullOrWhiteSpace(message.Reaction)
            ? "React to message"
            : "Change reaction";

        ShowOverlayFlyout(
            MessageActionsFlyout,
            position,
            245,
            185);
    }

    private void MessageReactFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (_activeMessageAction is null)
            return;

        MessageActionsFlyout.Visibility = Visibility.Collapsed;

        var position = GetFlyoutPosition(MessageActionsFlyout);
        ReactionCurrentText.Text = string.IsNullOrWhiteSpace(_activeMessageAction.Reaction)
            ? "Choose a reaction"
            : $"Current: {_activeMessageAction.Reaction}";
        RemoveReactionFlyoutButton.Visibility =
            string.IsNullOrWhiteSpace(_activeMessageAction.Reaction)
                ? Visibility.Collapsed
                : Visibility.Visible;

        ShowOverlayFlyout(
            ReactionActionsFlyout,
            new Point(position.X, position.Y),
            330,
            135);
    }

    private void ReactionChoice_Click(object sender, RoutedEventArgs e)
    {
        if (_activeMessageAction is null ||
            sender is not Button button ||
            button.Tag is not string reaction)
            return;

        _activeMessageAction.Reaction = reaction;
        CloseAllFlyouts();
        RenderCurrentConversation();
        MarkDirty();
    }

    private void RemoveReactionFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (_activeMessageAction is null)
            return;

        _activeMessageAction.Reaction = null;
        CloseAllFlyouts();
        RenderCurrentConversation();
        MarkDirty();
    }

    private void CopyMessageFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (_activeMessageAction is null)
            return;

        try
        {
            Clipboard.SetText(_activeMessageAction.Text);
        }
        catch
        {
            // Clipboard failures should not close the application.
        }

        CloseAllFlyouts();
    }

    private void DeleteMessageFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (_activeMessageAction is null)
            return;

        _selectedConversation?.Messages.Remove(_activeMessageAction);
        CloseAllFlyouts();
        _activeMessageAction = null;
        RenderCurrentConversation();
        RefreshConversationListPreservingSelection();
        MarkDirty();
    }

    private void ShowOverlayFlyout(
        FrameworkElement flyout,
        Point position,
        double width,
        double estimatedHeight)
    {
        OverlayCanvas.IsHitTestVisible = true;
        flyout.Width = width;
        flyout.Visibility = Visibility.Visible;
        flyout.IsHitTestVisible = true;

        var maxWidth = Math.Max(0, OverlayCanvas.ActualWidth - width - 12);
        var maxHeight = Math.Max(0, OverlayCanvas.ActualHeight - estimatedHeight - 12);

        var left = Math.Clamp(position.X + 10, 12, maxWidth);
        var top = Math.Clamp(position.Y + 8, 86, maxHeight);

        Canvas.SetLeft(flyout, left);
        Canvas.SetTop(flyout, top);
    }

    private Point GetFlyoutPosition(FrameworkElement flyout) =>
        new(
            Canvas.GetLeft(flyout),
            Canvas.GetTop(flyout));

    private void CloseAllFlyouts()
    {
        ConversationActionsFlyout.IsHitTestVisible = false;
        ConversationActionsFlyout.Visibility = Visibility.Collapsed;

        MessageActionsFlyout.IsHitTestVisible = false;
        MessageActionsFlyout.Visibility = Visibility.Collapsed;

        ReactionActionsFlyout.IsHitTestVisible = false;
        ReactionActionsFlyout.Visibility = Visibility.Collapsed;


        OverlayCanvas.IsHitTestVisible = false;
        _activeMessageAction = null;
    }

    private void CloseConversationActionsFlyout()
    {
        ConversationActionsFlyout.IsHitTestVisible = false;
        ConversationActionsFlyout.Visibility = Visibility.Collapsed;

        if (MessageActionsFlyout.Visibility != Visibility.Visible &&
            ReactionActionsFlyout.Visibility != Visibility.Visible)
        {
            OverlayCanvas.IsHitTestVisible = false;
        }
    }

    private string GetMessageBubbleColor(string senderId)
    {
        if (_selectedConversation is null)
            return ThemeService.GetIncomingBubbleColor(_project.ThemeId);

        if (senderId == "self")
        {
            return _selectedConversation.UseThemeBubbleColors
                ? ThemeService.GetOutgoingBubbleColor(_project.ThemeId)
                : _selectedConversation.OutgoingBubbleColor;
        }

        if (_selectedConversation.IsGroup)
        {
            var index = _selectedConversation.ParticipantIds
                .FindIndex(id => id == senderId);

            if (index >= 0)
                return ThemeService.GetParticipantBubbleColor(_project.ThemeId, index);
        }

        return _selectedConversation.UseThemeBubbleColors
            ? ThemeService.GetIncomingBubbleColor(_project.ThemeId)
            : _selectedConversation.IncomingBubbleColor;
    }

    private void ApplyThemeToConversations()
    {
        foreach (var conversation in _project.Conversations)
        {
            if (!conversation.UseThemeBubbleColors)
                continue;

            conversation.OutgoingBubbleColor =
                ThemeService.GetOutgoingBubbleColor(_project.ThemeId);

            conversation.IncomingBubbleColor =
                ThemeService.GetIncomingBubbleColor(_project.ThemeId);
        }
    }

    private Border CreateContactAvatar(ChatCharacter contact, double size)
    {
        var baseColor = ParseColor(contact.AvatarColor);

        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = CreateAvatarBrush(baseColor),
            BorderBrush = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
            BorderThickness = new Thickness(1)
        };

        border.Child = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(contact.AvatarIcon)
                ? contact.Initial
                : contact.AvatarIcon,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = string.IsNullOrWhiteSpace(contact.AvatarIcon)
                ? 15
                : 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        return border;
    }

    private Border CreateConversationAvatar(ChatConversation conversation)
    {
        var first = GetParticipants(conversation).FirstOrDefault();
        var baseColor = first is null
            ? Color.FromRgb(88, 166, 255)
            : ParseColor(first.AvatarColor);

        var border = new Border
        {
            Width = 50,
            Height = 50,
            CornerRadius = new CornerRadius(25),
            Background = CreateAvatarBrush(baseColor),
            BorderBrush = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
            BorderThickness = new Thickness(1)
        };

        border.Child = new TextBlock
        {
            Text = conversation.IsGroup
                ? "＋"
                : string.IsNullOrWhiteSpace(first?.AvatarIcon)
                    ? first?.Initial ?? "?"
                    : first.AvatarIcon,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = conversation.IsGroup
                ? 17
                : 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        return border;
    }

    private void UpdateHeaderAvatar(ChatCharacter? contact, bool isGroup)
    {
        var baseColor = contact is null
            ? Color.FromRgb(88, 166, 255)
            : ParseColor(contact.AvatarColor);

        CurrentAvatar.Background = CreateAvatarBrush(baseColor);
        CurrentAvatar.BorderBrush =
            new SolidColorBrush(Color.FromArgb(48, 255, 255, 255));
        CurrentAvatar.BorderThickness = new Thickness(1);

        CurrentAvatarText.Text = isGroup
            ? "＋"
            : string.IsNullOrWhiteSpace(contact?.AvatarIcon)
                ? contact?.Initial ?? "?"
                : contact.AvatarIcon;

        CurrentAvatarText.Foreground = Brushes.White;
        CurrentAvatarText.FontSize = isGroup ? 15 : 14;
    }

    private static Brush CreateAvatarBrush(Color baseColor)
    {
        var lighter = MixColor(baseColor, Colors.White, 0.22);
        var darker = MixColor(baseColor, Colors.Black, 0.18);

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };

        brush.GradientStops.Add(new GradientStop(lighter, 0.0));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.48));
        brush.GradientStops.Add(new GradientStop(darker, 1.0));

        return brush;
    }

    private static Color ParseColor(string color)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(color);
        }
        catch
        {
            return Color.FromRgb(88, 166, 255);
        }
    }

    private static Color MixColor(Color source, Color target, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);

        return Color.FromRgb(
            (byte)(source.R + ((target.R - source.R) * amount)),
            (byte)(source.G + ((target.G - source.G) * amount)),
            (byte)(source.B + ((target.B - source.B) * amount)));
    }

    private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingConversationList)
            return;

        if (ConversationList.SelectedItem is not ListBoxItem item)
            return;

        var id = item.Tag?.ToString();
        _selectedConversation = _project.Conversations.FirstOrDefault(c => c.Id == id);
        _project.ActiveConversationId = _selectedConversation?.Id;

        RenderCurrentConversation();
        MarkDirty();
    }

    private void RefreshConversationListPreservingSelection()
    {
        var activeId = _selectedConversation?.Id;
        _refreshingConversationList = true;

        try
        {
            RenderConversationList();

            if (string.IsNullOrWhiteSpace(activeId))
                return;

            foreach (var item in ConversationList.Items.OfType<ListBoxItem>())
            {
                if (string.Equals(item.Tag?.ToString(), activeId, StringComparison.Ordinal))
                {
                    ConversationList.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _refreshingConversationList = false;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderConversationList();
        RenderContactsList();
    }

    private void SendMessage_Click(object sender, RoutedEventArgs e) =>
        SendCurrentMessageAsync();

    private void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            SendCurrentMessageAsync();
        }
    }

    private void SendCurrentMessageAsync()
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

        // Update both the open chat and its sidebar preview immediately.
        RenderCurrentConversation();
        RefreshConversationListPreservingSelection();
        MarkDirty();

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
            PerspectiveId = "self",
            OutgoingBubbleColor = ThemeService.GetOutgoingBubbleColor(_project.ThemeId),
            IncomingBubbleColor = ThemeService.GetIncomingBubbleColor(_project.ThemeId),
            UseThemeBubbleColors = true
        };

        try
        {
            _project.Conversations.Insert(0, conversation);
            _project.ActiveConversationId = conversation.Id;
            _selectedConversation = conversation;

            _refreshingConversationList = true;
            try
            {
                RenderConversationList();

                var listItem = ConversationList.Items
                    .OfType<ListBoxItem>()
                    .FirstOrDefault(item => string.Equals(
                        item.Tag?.ToString(),
                        conversation.Id,
                        StringComparison.Ordinal));

                ConversationList.SelectedItem = listItem;
            }
            finally
            {
                _refreshingConversationList = false;
            }

            RenderContactsList();
            RenderCurrentConversation();
            UpdateComposerState();
            MarkDirty();
        }
        catch (Exception ex)
        {
            _project.Conversations.Remove(conversation);
            _project.ActiveConversationId = null;
            _selectedConversation = null;
            _refreshingConversationList = false;

            try
            {
                RenderAll();
                UpdateComposerState();
            }
            catch
            {
                // Keep the original failure as the useful diagnostic.
            }

            MessageBox.Show(
                this,
                $"The chat could not be created.\n\n{ex.GetType().Name}: {ex.Message}",
                "Chat Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
        ToggleSettingsView();
    }

    private string _settingsSelectedThemeId = ThemeService.Light;

    private void ToggleSettingsView()
    {
        if (SettingsView.Visibility == Visibility.Visible)
        {
            CloseSettingsView();
            return;
        }

        CloseAllFlyouts();
        _settingsSelectedThemeId = ThemeService.Normalize(_project.ThemeId);
        SettingsReadReceiptsCheckBox.IsChecked = _selectedConversation?.ShowReadReceipts ?? true;
        SettingsTimestampCheckBox.IsChecked = _selectedConversation?.ShowTimestamps ?? true;
        SettingsTypingCheckBox.IsChecked = _selectedConversation?.ShowTypingIndicators ?? true;
        SettingsUseThemeColorsCheckBox.IsChecked = _selectedConversation?.UseThemeBubbleColors ?? true;
        SettingsOutgoingColorBox.Text = _selectedConversation?.OutgoingBubbleColor
            ?? ThemeService.GetOutgoingBubbleColor(_settingsSelectedThemeId);
        SettingsIncomingColorBox.Text = _selectedConversation?.IncomingBubbleColor
            ?? ThemeService.GetIncomingBubbleColor(_settingsSelectedThemeId);

        SettingsThemeButtonsPanel.Children.Clear();
        foreach (var theme in ThemeService.GetThemeOptions())
        {
            var button = new Button
            {
                Tag = theme.Id,
                Width = 164,
                Height = 76,
                Margin = new Thickness(0, 0, 10, 10),
                Background = GetThemeBrush("PanelBackground"),
                BorderBrush = GetThemeBrush("Divider"),
                BorderThickness = new Thickness(1),
                Content = CreateSettingsThemePreview(theme)
            };

            button.Click += SettingsThemeButton_Click;
            SettingsThemeButtonsPanel.Children.Add(button);
        }

        RefreshSettingsThemeSelection();
        SettingsView.Visibility = Visibility.Visible;
    }

    private void CloseSettingsView()
    {
        SettingsView.Visibility = Visibility.Collapsed;
        ThemeService.ApplyTheme(_project.ThemeId);
        RenderAll();
        UpdateComposerState();
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e) =>
        CloseSettingsView();

    private static Grid CreateSettingsThemePreview(ThemeService.ThemeOption theme)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var swatches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        swatches.Children.Add(new Border
        {
            Width = 18,
            Height = 40,
            Background = ParseBrush(theme.PreviewBackground),
            BorderBrush = ParseBrush(theme.PreviewBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        });

        swatches.Children.Add(new Border
        {
            Width = 18,
            Height = 40,
            Background = ParseBrush(theme.PreviewAccent),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(-2, 0, 0, 0)
        });

        grid.Children.Add(swatches);

        var label = new TextBlock
        {
            Text = theme.Name,
            Tag = "theme-name",
            Foreground = GetThemeBrush("TextPrimary"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(label, 1);
        grid.Children.Add(label);
        return grid;
    }

    private void SettingsThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        _settingsSelectedThemeId = ThemeService.Normalize(button.Tag?.ToString());
        _project.ThemeId = _settingsSelectedThemeId;
        ThemeService.ApplyTheme(_settingsSelectedThemeId);

        if (SettingsUseThemeColorsCheckBox.IsChecked == true || _selectedConversation is null)
        {
            SettingsOutgoingColorBox.Text = ThemeService.GetOutgoingBubbleColor(_settingsSelectedThemeId);
            SettingsIncomingColorBox.Text = ThemeService.GetIncomingBubbleColor(_settingsSelectedThemeId);
        }

        RefreshSettingsThemeSelection();
        RenderAll();
    }

    private void RefreshSettingsThemeSelection()
    {
        foreach (var button in SettingsThemeButtonsPanel.Children.OfType<Button>())
        {
            var selected = string.Equals(button.Tag?.ToString(), _settingsSelectedThemeId, StringComparison.OrdinalIgnoreCase);
            button.Background = GetThemeBrush("PanelBackground");
            button.BorderBrush = selected ? GetThemeBrush("Accent") : GetThemeBrush("Divider");
            button.BorderThickness = selected ? new Thickness(2.5) : new Thickness(1);

            if (button.Content is Grid preview)
            {
                var label = preview.Children.OfType<TextBlock>()
                    .FirstOrDefault(t => string.Equals(t.Tag?.ToString(), "theme-name", StringComparison.Ordinal));
                if (label is not null)
                    label.Foreground = GetThemeBrush("TextPrimary");
            }
        }
    }

    private void SettingsUseThemeColorsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsUseThemeColorsCheckBox.IsChecked != true)
            return;

        SettingsOutgoingColorBox.Text = ThemeService.GetOutgoingBubbleColor(_settingsSelectedThemeId);
        SettingsIncomingColorBox.Text = ThemeService.GetIncomingBubbleColor(_settingsSelectedThemeId);
    }

    private void DoneSettings_Click(object sender, RoutedEventArgs e)
    {
        var outgoing = SettingsOutgoingColorBox.Text.Trim();
        var incoming = SettingsIncomingColorBox.Text.Trim();

        if (!IsValidSettingsColor(outgoing) || !IsValidSettingsColor(incoming))
        {
            MessageBox.Show(this, "Enter valid hex colors such as #0A84FF.", "Settings",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _project.ThemeId = ThemeService.Normalize(_settingsSelectedThemeId);
        ThemeService.ApplyTheme(_project.ThemeId);

        if (_selectedConversation is not null)
        {
            var useThemeColors = SettingsUseThemeColorsCheckBox.IsChecked == true;
            _selectedConversation.ShowReadReceipts = SettingsReadReceiptsCheckBox.IsChecked == true;
            _selectedConversation.ShowTimestamps = SettingsTimestampCheckBox.IsChecked == true;
            _selectedConversation.ShowTypingIndicators = SettingsTypingCheckBox.IsChecked == true;
            _selectedConversation.UseThemeBubbleColors = useThemeColors;
            _selectedConversation.OutgoingBubbleColor = useThemeColors
                ? ThemeService.GetOutgoingBubbleColor(_project.ThemeId)
                : outgoing;
            _selectedConversation.IncomingBubbleColor = useThemeColors
                ? ThemeService.GetIncomingBubbleColor(_project.ThemeId)
                : incoming;
        }

        ApplyThemeToConversations();
        SettingsView.Visibility = Visibility.Collapsed;
        RenderAll();
        UpdateComposerState();
        MarkDirty();
    }

    private static bool IsValidSettingsColor(string value)
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
        ToggleConversationActionsFlyout();
    }

    private void ToggleConversationActionsFlyout()
    {
        if (_selectedConversation is null)
            return;

        var isOpening = ConversationActionsFlyout.Visibility != Visibility.Visible;

        if (!isOpening)
        {
            CloseConversationActionsFlyout();
            return;
        }

        MessageActionsFlyout.Visibility = Visibility.Collapsed;
        ReactionActionsFlyout.Visibility = Visibility.Collapsed;
        _activeMessageAction = null;

        ConversationActionsCountText.Text = _selectedConversation.Messages.Count == 0
            ? "No messages yet"
            : $"{_selectedConversation.Messages.Count} message{(_selectedConversation.Messages.Count == 1 ? "" : "s")}";

        ShowOverlayFlyout(
            ConversationActionsFlyout,
            new Point(OverlayCanvas.ActualWidth, 0),
            280,
            245);
    }

    private void CustomizeChatFlyout_Click(object sender, RoutedEventArgs e)
    {
        CloseConversationActionsFlyout();
        ToggleSettingsView();
    }

    private void ExportChatFlyout_Click(object sender, RoutedEventArgs e)
    {
        CloseConversationActionsFlyout();
        Export_Click(this, new RoutedEventArgs());
    }

    private void DeleteChatFlyout_Click(object sender, RoutedEventArgs e)
    {
        CloseConversationActionsFlyout();
        DeleteConversation_Click(this, new RoutedEventArgs());
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        CloseAllFlyouts();
    }

    private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;

        if (IsInsideElement(source, ConversationActionsFlyout) ||
            IsInsideElement(source, MessageActionsFlyout) ||
            IsInsideElement(source, ReactionActionsFlyout) ||
            IsInsideElement(source, MoreButton))
            return;

        CloseAllFlyouts();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        if (SettingsView.Visibility == Visibility.Visible)
        {
            CloseSettingsView();
            e.Handled = true;
            return;
        }

        CloseAllFlyouts();
        e.Handled = true;
    }

    private static bool IsInsideElement(DependencyObject? source, DependencyObject target)
    {
        var current = source;

        while (current is not null)
        {
            if (ReferenceEquals(current, target))
                return true;

            current = current switch
            {
                Visual visual => VisualTreeHelper.GetParent(visual),
                FrameworkContentElement content => content.Parent,
                _ => null
            };
        }

        return false;
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
        SendAsButton.IsEnabled = enabled;
        SendAsIndicator.Opacity = enabled ? 1 : 0.45;
    }

    private static bool IsLightColor(string color)
    {
        try
        {
            var value = (Color)ColorConverter.ConvertFromString(color);
            var luminance = (0.299 * value.R) + (0.587 * value.G) + (0.114 * value.B);
            return luminance > 182;
        }
        catch
        {
            return false;
        }
    }

    private static SolidColorBrush GetThemeBrush(string key) =>
        Application.Current.Resources[key] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Transparent);

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in System.IO.Path.GetInvalidFileNameChars())
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