using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FakeChatStudio.Models;
using FakeChatStudio.Services;

namespace FakeChatStudio;

public partial class MainWindow : Window
{
    private readonly ProjectStorageService _storage = new();
    private ChatProject _project;
    private ChatMessage? _selectedMessage;

    public MainWindow()
    {
        InitializeComponent();
        _project = _storage.Load();
        ProjectNameText.Text = _project.Name;
        RefreshSenderCombo();
        RenderCharacters();
        RenderMessages();
        UpdateConversationHeader();
    }

    private void RenderCharacters()
    {
        CharactersPanel.Children.Clear();

        foreach (var character in _project.Characters)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151820")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 7),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatar = new Border
            {
                Width = 36, Height = 36, CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(character.Color))
            };
            avatar.Child = new TextBlock
            {
                Text = character.Initial, FontSize = 15, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };

            var text = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = character.Name, FontWeight = FontWeights.SemiBold });
            text.Children.Add(new TextBlock { Text = character.Role, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(137,147,164)) });

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(text, 1);
            grid.Children.Add(avatar);
            grid.Children.Add(text);
            border.Child = grid;

            border.MouseLeftButtonUp += (_, _) => SelectCharacter(character);
            CharactersPanel.Children.Add(border);
        }
    }

    private void SelectCharacter(ChatCharacter character)
    {
        SenderCombo.SelectedItem = character.Name;
    }

    private void RefreshSenderCombo()
    {
        SenderCombo.Items.Clear();

        foreach (var character in _project.Characters)
            SenderCombo.Items.Add(character.Name);

        if (SenderCombo.Items.Count > 0)
            SenderCombo.SelectedIndex = 0;
    }

    private void RenderMessages()
    {
        MessagesPanel.Children.Clear();

        if (_project.Messages.Count == 0)
        {
            var emptyState = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 360
            };

            emptyState.Children.Add(new TextBlock
            {
                Text = "✦",
                FontSize = 34,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 99, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });

            emptyState.Children.Add(new TextBlock
            {
                Text = "No messages yet",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            emptyState.Children.Add(new TextBlock
            {
                Text = _project.Characters.Count == 0
                    ? "Add a character, then create your first message."
                    : "Create your first message to start the conversation.",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(137, 147, 164)),
                Margin = new Thickness(0, 8, 0, 0)
            });

            MessagesPanel.Children.Add(emptyState);
            return;
        }

        foreach (var message in _project.Messages)
        {
            var isMine = message.Sender == "Sara";
            var wrapper = new StackPanel
            {
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = isMine
                    ? new Thickness { Left = 120, Bottom = 10 }
                    : new Thickness { Right = 120, Bottom = 10 },
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var bubble = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isMine ? "#655DEB" : "#1B202A")),
                CornerRadius = isMine ? new CornerRadius(16,16,4,16) : new CornerRadius(16,16,16,4),
                Padding = new Thickness(15, 10, 15, 10)
            };
            bubble.Child = new TextBlock { Text = message.Text, FontSize = 14, TextWrapping = TextWrapping.Wrap };

            var meta = new TextBlock
            {
                Text = $"{message.Time}  •  {message.Sender}" + (isMine && message.IsRead ? "  ✓✓" : ""),
                FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(101,112,131)),
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = isMine ? new Thickness(0, 5, 5, 0) : new Thickness(5, 5, 0, 0)
            };

            wrapper.Children.Add(bubble);
            wrapper.Children.Add(meta);
            wrapper.MouseLeftButtonUp += (_, _) => SelectMessage(message);
            MessagesPanel.Children.Add(wrapper);
        }
    }

    private void SelectMessage(ChatMessage message)
    {
        _selectedMessage = message;
        MessageTextBox.Text = message.Text;
        TimeTextBox.Text = message.Time;
        SenderCombo.SelectedItem = message.Sender;
        ReadCheckBox.IsChecked = message.IsRead;
    }

    private void NewMessage_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Characters.Count == 0)
        {
            MessageBox.Show(
                "Add a character before creating your first message.",
                "Create a character",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var senderName = SenderCombo.SelectedItem?.ToString();
        var text = MessageInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        _project.Messages.Add(new ChatMessage
        {
            Sender = senderName,
            Text = text,
            Time = DateTime.Now.ToString("HH:mm"),
            IsRead = true
        });

        MessageInput.Clear();
        RenderMessages();
        UpdateConversationHeader();
        SelectMessage(_project.Messages[^1]);
    }

    private void ApplyMessage_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMessage is null)
            return;

        _selectedMessage.Text = MessageTextBox.Text;
        _selectedMessage.Time = TimeTextBox.Text;
        _selectedMessage.Sender = SenderCombo.SelectedItem?.ToString() ?? _selectedMessage.Sender;
        _selectedMessage.IsRead = ReadCheckBox.IsChecked == true;
        RenderMessages();
        UpdateConversationHeader();
        SelectMessage(_selectedMessage);
    }

    private void DeleteMessage_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMessage is null)
            return;

        _project.Messages.Remove(_selectedMessage);
        _selectedMessage = null;
        MessageTextBox.Clear();
        RenderMessages();
        UpdateConversationHeader();
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        _project.Name = ProjectNameText.Text;
        _storage.Save(_project);
        SavedText.Text = "Saved";
    }

    private void AddCharacter_Click(object sender, RoutedEventArgs e)
    {
        var character = new ChatCharacter
        {
            Name = $"Character {_project.Characters.Count + 1}",
            Role = "New character",
            Initial = $"{_project.Characters.Count + 1}",
            Color = "#6C63FF"
        };

        _project.Characters.Add(character);
        RenderCharacters();
        RefreshSenderCombo();
        SenderCombo.SelectedItem = character.Name;
    }

    private void UpdateConversationHeader()
    {
        if (_project.Messages.Count == 0)
        {
            ConversationTitleText.Text = "New conversation";
            ConversationSubtitleText.Text =
                _project.Characters.Count == 0
                    ? "Add characters to begin"
                    : "No messages yet";
            return;
        }

        var names = _project.Characters
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Take(2)
            .ToList();

        ConversationTitleText.Text = names.Count switch
        {
            0 => "Conversation",
            1 => names[0],
            _ => $"{names[0]} & {names[1]}"
        };

        ConversationSubtitleText.Text =
            $"{_project.Messages.Count} message{(_project.Messages.Count == 1 ? "" : "s")}  •  " +
            $"{names.Count} participant{(names.Count == 1 ? "" : "s")}";
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "PNG export is the next export feature. Your project is already saved locally.",
            "FakeChat Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}