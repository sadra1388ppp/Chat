using System.Windows;
using Chat.Models;

namespace Chat.Dialogs;

public partial class CreateConversationWindow : Window
{
    public string ConversationTitle { get; private set; } = "";
    public string Scenario { get; private set; } = "";
    public bool AiEnabled { get; private set; }
    public List<string> ParticipantIds { get; private set; } = [];

    public CreateConversationWindow(IReadOnlyList<ChatCharacter> contacts)
    {
        InitializeComponent();
        ContactsList.ItemsSource = contacts;
        ContactsList.DisplayMemberPath = nameof(ChatCharacter.Name);

        if (contacts.Count == 1)
            ContactsList.SelectedIndex = 0;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var selected = ContactsList.SelectedItems.Cast<ChatCharacter>().ToList();

        if (selected.Count == 0)
        {
            HintText.Text = "Choose at least one participant.";
            HintText.Foreground = System.Windows.Media.Brushes.IndianRed;
            return;
        }

        ParticipantIds = selected.Select(c => c.Id).ToList();
        ConversationTitle = TitleBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ConversationTitle))
        {
            ConversationTitle = selected.Count == 1
                ? selected[0].Name
                : string.Join(", ", selected.Take(3).Select(c => c.Name));
        }

        Scenario = ScenarioBox.Text.Trim();
        AiEnabled = AiCheckBox.IsChecked == true || selected.Any(c => c.IsAi);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}