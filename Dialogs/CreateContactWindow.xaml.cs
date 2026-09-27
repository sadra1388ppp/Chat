using System.Windows;
using System.Windows.Media;
using Chat.Models;

namespace Chat.Dialogs;

public partial class CreateContactWindow : Window
{
    public ChatCharacter? CreatedContact { get; private set; }

    public CreateContactWindow()
    {
        InitializeComponent();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Enter a contact name.", "Create Contact", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var color = ColorBox.Text.Trim();
        if (!IsValidColor(color))
        {
            MessageBox.Show(this, "Use a valid hex color such as #0A84FF.", "Create Contact", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CreatedContact = new ChatCharacter
        {
            Name = name,
            Role = string.IsNullOrWhiteSpace(RoleBox.Text) ? "Contact" : RoleBox.Text.Trim(),
            Context = ContextBox.Text.Trim(),
            Initial = BuildInitial(name),
            AvatarColor = color,
            BubbleColor = "#E9EDF2",
            IsAi = AiCheckBox.IsChecked == true
        };

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

    private static string BuildInitial(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : name[..1].ToUpperInvariant();
    }
}