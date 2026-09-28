using System.Windows;
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
        ContactsList.ItemsSource = contacts;
        ContactsList.DisplayMemberPath = nameof(ChatCharacter.Name);

        SetMode(false);

        if (contacts.Count == 1)
            ContactsList.SelectedIndex = 0;

        ContactsList.SelectionChanged += (_, _) => UpdateSelectionSummary();
        UpdateSelectionSummary();
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
            var first = ContactsList.SelectedItems[0] as ChatCharacter;
            ContactsList.SelectedItems.Clear();

            if (first is not null)
            {
                first = _contacts.FirstOrDefault(c => c.Id == first.Id);
                if (first is not null)
                {
                    var index = _contacts.ToList().FindIndex(c => c.Id == first.Id);
                    if (index >= 0)
                        ContactsList.SelectedIndex = index;
                }
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
        var selected = ContactsList.SelectedItems
            .Cast<ChatCharacter>()
            .ToList();

        SelectionSummaryText.Text = selected.Count == 0
            ? (_isGroupMode ? "No group members selected" : "No contact selected")
            : _isGroupMode
                ? $"{selected.Count} members selected"
                : selected[0].Name;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var selected = ContactsList.SelectedItems
            .Cast<ChatCharacter>()
            .ToList();

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
}
