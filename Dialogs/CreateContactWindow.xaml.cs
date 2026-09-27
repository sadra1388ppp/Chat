using System.Windows;
using System.Windows.Media;
using Chat.Models;

namespace Chat.Dialogs;

public partial class CreateContactWindow : Window
{
    private static readonly string[] AvatarIcons =
    [
        "●", "＋", "★", "♥", "☎", "✉", "⌂", "☁",
        "●", "✦", "⚙", "♣", "◆", "☺", "☀", "☕"
    ];

    private string _selectedAvatar = "●";

    public ChatCharacter? CreatedContact { get; private set; }

    public CreateContactWindow()
    {
        InitializeComponent();
        BuildAvatarChoices();
    }

    private void BuildAvatarChoices()
    {
        AvatarChoices.Children.Clear();

        foreach (var icon in AvatarIcons)
        {
            var button = new System.Windows.Controls.Button
            {
                Content = icon,
                Tag = icon,
                Width = 44,
                Height = 44,
                Margin = new Thickness(4),
                Background = new SolidColorBrush(Color.FromRgb(221, 245, 232)),
                Foreground = new SolidColorBrush(Color.FromRgb(21, 148, 71)),
                BorderThickness = new Thickness(0),
                FontSize = 17,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            button.Click += (_, _) =>
            {
                _selectedAvatar = icon;

                foreach (var child in AvatarChoices.Children)
                {
                    if (child is System.Windows.Controls.Button other)
                    {
                        other.Background = Equals(other.Tag?.ToString(), _selectedAvatar)
                            ? new SolidColorBrush(Color.FromRgb(190, 238, 207))
                            : new SolidColorBrush(Color.FromRgb(221, 245, 232));
                    }
                }
            };

            AvatarChoices.Children.Add(button);
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                this,
                "Enter a contact name.",
                "Create Contact",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var color = ColorBox.Text.Trim();

        if (!IsValidColor(color))
        {
            MessageBox.Show(
                this,
                "Use a valid hex color such as #E9EDF2.",
                "Create Contact",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        CreatedContact = new ChatCharacter
        {
            Name = name,
            Role = string.IsNullOrWhiteSpace(RoleBox.Text)
                ? "Contact"
                : RoleBox.Text.Trim(),
            PhoneNumber = PhoneBox.Text.Trim(),
            Context = ContextBox.Text.Trim(),
            Initial = BuildInitial(name),
            AvatarIcon = _selectedAvatar,
            AvatarColor = ColorBox.Text.Trim(),
            BubbleColor = color,
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