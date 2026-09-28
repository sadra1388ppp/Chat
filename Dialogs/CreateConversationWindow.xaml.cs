using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chat.Models;
using Chat.Services;

namespace Chat.Dialogs;

public partial class CreateConversationWindow : Window
{
    private readonly IReadOnlyList<ChatCharacter> _contacts;
    private bool _isGroupMode;

    public string ConversationTitle { get; private set; } = "";
    public string Scenario { get; private set; } = "";
    public bool AiEnabled { get; private set; }
    public List<string> ParticipantIds { get; private set; } = [];

    public CreateConversationWindow(IReadOnlyList<ChatCharacter> contacts)
    {
        InitializeComponent();

        _contacts = contacts;
        BuildContactList();

        SetMode(false);

        if (contacts.Count == 1)
            ContactsList.SelectedIndex = 0;

        ContactsList.SelectionChanged += (_, _) => UpdateSelectionSummary();
        UpdateSelectionSummary();
    }

    private void BuildContactList()
    {
        ContactsList.Items.Clear();

        foreach (var contact in _contacts)
        {
            var avatar = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = ParseBrush(contact.AvatarColor),
                VerticalAlignment = VerticalAlignment.Center
            };

            avatar.Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(contact.AvatarIcon)
                    ? contact.Initial
                    : contact.AvatarIcon,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = string.IsNullOrWhiteSpace(contact.AvatarIcon)
                    ? Brushes.White
                    : ThemeBrush("Accent"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var name = new TextBlock
            {
                Text = contact.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = ThemeBrush("TextPrimary"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var subtitle = new TextBlock
            {
                Text = contact.IsAi
                    ? $"{contact.Role} • AI"
                    : contact.Role,
                FontSize = 10.5,
                Foreground = ThemeBrush("TextSecondary"),
                Margin = new Thickness(0, 3, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var text = new StackPanel
            {
                Margin = new Thickness(11, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            text.Children.Add(name);
            text.Children.Add(subtitle);

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
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
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            });
        }
    }

    private void ChatMode_Click(object sender, RoutedEventArgs e) => SetMode(false);

    private void GroupMode_Click(object sender, RoutedEventArgs e) => SetMode(true);

    private void SetMode(bool groupMode)
    {
        _isGroupMode = groupMode;

        ContactsList.SelectionMode =
            groupMode ? SelectionMode.Multiple : SelectionMode.Single;

        if (!groupMode && ContactsList.SelectedItems.Count > 1)
        {
            var selectedId = (ContactsList.SelectedItems[0] as ListBoxItem)?.Tag?.ToString();
            ContactsList.SelectedItems.Clear();

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                var index = _contacts.ToList().FindIndex(c => c.Id == selectedId);
                if (index >= 0)
                    ContactsList.SelectedIndex = index;
            }
        }

        ChatModeButton.Background = groupMode
            ? ThemeBrush("PanelBackground")
            : ThemeBrush("AccentSoft");
        ChatModeButton.BorderBrush = groupMode
            ? ThemeBrush("Divider")
            : ThemeBrush("Accent");

        GroupModeButton.Background = groupMode
            ? ThemeBrush("AccentSoft")
            : ThemeBrush("PanelBackground");
        GroupModeButton.BorderBrush = groupMode
            ? ThemeBrush("Accent")
            : ThemeBrush("Divider");

        ParticipantHeaderText.Text = groupMode ? "GROUP MEMBERS" : "CONTACT";
        ParticipantModeHint.Text = groupMode ? "Select two or more" : "Select one";
        CreateButton.Content = groupMode ? "Create Group" : "Create Chat";
        HintText.Text = groupMode
            ? "Select at least two contacts."
            : "Select one contact.";
        HintText.Foreground = ThemeBrush("TextSecondary");

        UpdateSelectionSummary();
    }

    private void UpdateSelectionSummary()
    {
        var selected = GetSelectedContacts();

        SelectionSummaryText.Text = selected.Count == 0
            ? (_isGroupMode ? "No group members selected" : "No contact selected")
            : _isGroupMode
                ? $"{selected.Count} members selected"
                : selected[0].Name;
    }

    private List<ChatCharacter> GetSelectedContacts() =>
        ContactsList.SelectedItems
            .OfType<ListBoxItem>()
            .Select(item => item.Tag?.ToString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => _contacts.FirstOrDefault(c => c.Id == id))
            .Where(c => c is not null)
            .Cast<ChatCharacter>()
            .ToList();

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedContacts();

        var valid = _isGroupMode
            ? selected.Count >= 2
            : selected.Count == 1;

        if (!valid)
        {
            HintText.Text = _isGroupMode
                ? "A group needs at least two contacts."
                : "Choose exactly one contact.";
            HintText.Foreground = ThemeBrush("Danger");
            return;
        }

        ParticipantIds = selected.Select(c => c.Id).ToList();
        ConversationTitle = TitleBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ConversationTitle))
        {
            ConversationTitle = _isGroupMode
                ? string.Join(", ", selected.Take(3).Select(c => c.Name))
                : selected[0].Name;
        }

        Scenario = ScenarioBox.Text.Trim();
        AiEnabled = AiCheckBox.IsChecked == true || selected.Any(c => c.IsAi);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static SolidColorBrush ThemeBrush(string key) =>
        Application.Current.Resources[key] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Transparent);

    private static SolidColorBrush ParseBrush(string color)
    {
        try
        {
            var parsed = (Color)ColorConverter.ConvertFromString(color);
            return new SolidColorBrush(parsed);
        }
        catch
        {
            return ThemeBrush("Accent");
        }
    }
}
