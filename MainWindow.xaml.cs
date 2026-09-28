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

            ConversationList.Items.Add(item);
        }

        SidebarStatusText.Text = $"{_project.Conversations.Count} conversation{(_project.Conversations.Count == 1 ? "" : "s")}";
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
                Text = string.IsNullOrWhiteSpace(contact.AvatarIcon)
                    ? contact.Initial
                    : contact.AvatarIcon,
                Foreground = string.IsNullOrWhiteSpace(contact.AvatarIcon)
                    ? Brushes.White
                    : GetThemeBrush("Accent"),
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

            CurrentAvatarText.Text = "?";
            CurrentAvatar.Background = GetThemeBrush("AccentSoft");
            CurrentAvatarText.Foreground = GetThemeBrush("Accent");

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
        ConversationSubtitleText.Text = _selectedConversation.IsGroup
            ? $"{participants.Count} participants"
            : first?.Name ?? "Practice conversation";

        CurrentAvatarText.Text = _selectedConversation.IsGroup
            ? "＋"
            : string.IsNullOrWhiteSpace(first?.AvatarIcon)
                ? first?.Initial ?? "?"
                : first.AvatarIcon;

        CurrentAvatar.Background = first is null
            ? GetThemeBrush("AccentSoft")
            : ParseBrush(first.AvatarColor);

        CurrentAvatarText.Foreground = first is not null &&
            !string.IsNullOrWhiteSpace(first.AvatarIcon)
                ? GetThemeBrush("Accent")
                : Brushes.White;

        AiButton.Visibility = Visibility.Visible;
        AiButton.Content = _selectedConversation.IsAiEnabled ? "AI On" : "AI";
        AiButton.Background = _selectedConversation.IsAiEnabled
            ? GetThemeBrush("AccentSoft")
            : GetThemeBrush("SoftPanel");
        AiButton.BorderBrush = _selectedConversation.IsAiEnabled
            ? GetThemeBrush("Accent")
            : GetThemeBrush("Divider");
        AiButton.Foreground = _selectedConversation.IsAiEnabled
            ? GetThemeBrush("Accent")
            : GetThemeBrush("TextPrimary");
        AiButton.ToolTip = _selectedConversation.IsAiEnabled
            ? "AI conversation mode is on"
            : "Turn on AI conversation mode";

        if (_selectedConversation.IsAiEnabled)
        {
            var aiParticipant = participants.FirstOrDefault(c => c.IsAi);
            ConversationSubtitleText.Text = aiParticipant is null
                ? "AI mode enabled"
                : $"AI • {aiParticipant.Role}";
        }

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
        var isSticker = message.Kind == ChatMessageKind.Sticker;
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
            Padding = isSticker
                ? new Thickness(7)
                : new Thickness(14, 10, 14, 10),
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
            FontSize = isSticker ? 40 : 14,
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

        var reactionButton = new Button
        {
            Content = new TextBlock
            {
                Text = "☺",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            Width = 30,
            Height = 30,
            Padding = new Thickness(0),
            Margin = isOutgoing
                ? new Thickness(7, 0, 0, 0)
                : new Thickness(0, 0, 7, 0),
            Style = (Style)FindResource("IconButton"),
            ToolTip = "Add reaction"
        };

        reactionButton.ContextMenu = CreateReactionMenu(message, reactionButton);

        if (isOutgoing)
        {
            messageRow.Children.Add(bubble);
            messageRow.Children.Add(reactionButton);
        }
        else
        {
            messageRow.Children.Add(reactionButton);
            messageRow.Children.Add(bubble);
        }

        wrapper.Children.Add(messageRow);

        if (!string.IsNullOrWhiteSpace(message.Reaction))
        {
            var reactionPill = new Border
            {
                Background = GetThemeBrush("AccentSoft"),
                BorderBrush = GetThemeBrush("Divider"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(7, 3, 7, 3),
                HorizontalAlignment = isOutgoing
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left,
                Margin = isOutgoing
                    ? new Thickness(0, 4, 38, 0)
                    : new Thickness(38, 4, 0, 0),
                ToolTip = "Click the reaction button to change it"
            };

            reactionPill.Child = new TextBlock
            {
                Text = message.Reaction,
                FontSize = 13
            };

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

        var menu = new ContextMenu();
        menu.Items.Add(CreateStyledMenuItem("Copy", "Copy message", (_, _) =>
        {
            Clipboard.SetText(message.Text);
        }));
        menu.Items.Add(CreateStyledMenuItem(
            message.Reaction is null ? "React" : "Change reaction",
            "Choose a reaction",
            (_, _) => reactionButton.ContextMenu!.IsOpen = true));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateStyledMenuItem("Delete", "Delete message", (_, _) =>
        {
            _selectedConversation.Messages.Remove(message);
            RenderCurrentConversation();
            RefreshConversationListPreservingSelection();
            MarkDirty();
        }, destructive: true));

        wrapper.ContextMenu = menu;

        return wrapper;
    }

    private ContextMenu CreateReactionMenu(ChatMessage message, UIElement placementTarget)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = placementTarget,
            StaysOpen = false
        };

        var header = new MenuItem
        {
            Header = "Quick reactions",
            IsEnabled = false
        };
        menu.Items.Add(header);

        var reactions = new[] { "❤️", "👍", "😂", "😮", "😢", "🔥" };

        foreach (var emoji in reactions)
        {
            var item = CreateStyledMenuItem(emoji, "React with " + emoji, (_, _) =>
            {
                message.Reaction = emoji;
                RenderMessages();
                MarkDirty();
            });

            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateStyledMenuItem("×", "Remove reaction", (_, _) =>
        {
            message.Reaction = null;
            RenderMessages();
            MarkDirty();
        }, destructive: true));

        return menu;
    }

    private MenuItem CreateStyledMenuItem(
        string glyph,
        string text,
        RoutedEventHandler click,
        bool destructive = false)
    {
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        header.Children.Add(new TextBlock
        {
            Text = glyph,
            Width = 28,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = destructive
                ? GetThemeBrush("Danger")
                : GetThemeBrush("Accent")
        });

        header.Children.Add(new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = destructive
                ? GetThemeBrush("Danger")
                : GetThemeBrush("TextPrimary")
        });

        var item = new MenuItem
        {
            Header = header,
            Foreground = destructive
                ? GetThemeBrush("Danger")
                : GetThemeBrush("TextPrimary")
        };

        item.Click += click;
        return item;
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
            Text = conversation.IsGroup
                ? "＋"
                : string.IsNullOrWhiteSpace(first?.AvatarIcon)
                    ? first?.Initial ?? "?"
                    : first.AvatarIcon,
            Foreground = first is null || !string.IsNullOrWhiteSpace(first?.AvatarIcon)
                ? GetThemeBrush("Accent")
                : Brushes.White,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return border;
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

    private async void SendMessage_Click(object sender, RoutedEventArgs e) =>
        await SendCurrentMessageAsync();

    private async void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
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

        // Update both the open chat and its sidebar preview immediately.
        RenderCurrentConversation();
        RefreshConversationListPreservingSelection();
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
            Text = AiResponseService.Generate(
                aiContact,
                message,
                _selectedConversation.Scenario,
                _selectedConversation.Messages),
            Timestamp = DateTime.Now,
            IsRead = true
        });

        // Refresh the conversation card as soon as the AI reply arrives.
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
            IsAiEnabled = dialog.AiEnabled,
            PerspectiveId = "self",
            OutgoingBubbleColor = ThemeService.GetOutgoingBubbleColor(_project.ThemeId),
            IncomingBubbleColor = ThemeService.GetIncomingBubbleColor(_project.ThemeId),
            UseThemeBubbleColors = true
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
        var dialog = new SettingsWindow(_selectedConversation)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            _project.ThemeId = dialog.SelectedThemeId;
            ThemeService.ApplyTheme(_project.ThemeId);
            ApplyThemeToConversations();

            RenderAll();
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

        var title = new MenuItem
        {
            Header = $"Chat • {_selectedConversation.Title}",
            IsEnabled = false
        };
        menu.Items.Add(title);
        menu.Items.Add(new Separator());

        menu.Items.Add(CreateStyledMenuItem(
            "✦",
            "Customize chat",
            Settings_Click));

        menu.Items.Add(CreateStyledMenuItem(
            "↗",
            "Export as PNG",
            Export_Click));

        if (_selectedConversation.IsAiEnabled)
        {
            menu.Items.Add(CreateStyledMenuItem(
                "AI",
                "AI conversation is on",
                (_, _) => { }));
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateStyledMenuItem(
            "×",
            "Delete conversation",
            DeleteConversation_Click,
            destructive: true));

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