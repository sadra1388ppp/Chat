using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chat.Models;
using Chat.Services;

namespace Chat.Dialogs;

public partial class SettingsWindow : Window
{
    private readonly ChatConversation? _conversation;
    private readonly string _originalThemeId;

    public string SelectedThemeId { get; private set; }

    public SettingsWindow(ChatConversation? conversation)
    {
        InitializeComponent();

        _conversation = conversation;
        _originalThemeId = ThemeService.CurrentThemeId;
        SelectedThemeId = _originalThemeId;

        ReadReceiptsCheckBox.IsChecked = conversation?.ShowReadReceipts ?? true;
        TimestampCheckBox.IsChecked = conversation?.ShowTimestamps ?? true;
        TypingCheckBox.IsChecked = conversation?.ShowTypingIndicators ?? true;
        UseThemeColorsCheckBox.IsChecked = conversation?.UseThemeBubbleColors ?? true;

        OutgoingColorBox.Text = conversation?.OutgoingBubbleColor
            ?? ThemeService.GetOutgoingBubbleColor(SelectedThemeId);

        IncomingColorBox.Text = conversation?.IncomingBubbleColor
            ?? ThemeService.GetIncomingBubbleColor(SelectedThemeId);

        BuildThemeButtons();
        RefreshThemeSelection();
    }

    private void BuildThemeButtons()
    {
        ThemeButtonsPanel.Children.Clear();

        foreach (var theme in ThemeService.GetThemeOptions())
        {
            var button = new Button
            {
                Tag = theme.Id,
                Width = 164,
                Height = 76,
                Margin = new Thickness(0, 0, 10, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PreviewBackground)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PreviewBorder)),
                BorderThickness = new Thickness(1.5),
                Content = CreateThemePreview(theme)
            };

            button.Click += ThemeButton_Click;
            ThemeButtonsPanel.Children.Add(button);
        }
    }

    private static Grid CreateThemePreview(ThemeService.ThemeOption theme)
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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PreviewBackground)),
            CornerRadius = new CornerRadius(6),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PreviewBorder)),
            BorderThickness = new Thickness(1)
        });

        swatches.Children.Add(new Border
        {
            Width = 18,
            Height = 40,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PreviewAccent)),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(-2, 0, 0, 0)
        });

        grid.Children.Add(swatches);

        var label = new TextBlock
        {
            Text = theme.Name,
            Tag = "theme-name",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PreviewText))
        };

        Grid.SetColumn(label, 1);
        grid.Children.Add(label);

        return grid;
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        SelectedThemeId = ThemeService.Normalize(button.Tag?.ToString());
        ThemeService.ApplyTheme(SelectedThemeId);

        if (UseThemeColorsCheckBox.IsChecked == true || _conversation is null)
        {
            OutgoingColorBox.Text = ThemeService.GetOutgoingBubbleColor(SelectedThemeId);
            IncomingColorBox.Text = ThemeService.GetIncomingBubbleColor(SelectedThemeId);
        }

        RefreshThemeSelection();
    }

    private void RefreshThemeSelection()
    {
        var options = ThemeService.GetThemeOptions();

        foreach (var button in ThemeButtonsPanel.Children.OfType<Button>())
        {
            var selected = string.Equals(
                button.Tag?.ToString(),
                SelectedThemeId,
                StringComparison.OrdinalIgnoreCase);

            var theme = options.FirstOrDefault(t =>
                string.Equals(t.Id, button.Tag?.ToString(), StringComparison.OrdinalIgnoreCase));

            if (theme is null)
                continue;

            // Keep every theme card on the same surface.
            // Selection is shown only by the border, so the gallery stays visually uniform.
            button.Background = ThemeBrush("PanelBackground");

            button.BorderBrush = selected
                ? ThemeBrush("Accent")
                : ThemeBrush("Divider");

            button.BorderThickness = selected
                ? new Thickness(2.5)
                : new Thickness(1);

            if (button.Content is Grid preview)
            {
                var label = preview.Children
                    .OfType<TextBlock>()
                    .FirstOrDefault(t => string.Equals(
                        t.Tag?.ToString(),
                        "theme-name",
                        StringComparison.Ordinal));

                if (label is not null)
                {
                    // Theme names always use the current app text color.
                    // They never inherit a preview theme's text color, so every
                    // name stays readable on both light and dark app themes.
                    label.Foreground = ThemeBrush("TextPrimary");
                }
            }
        }
    }

    private static Color ParseColor(string value) =>
        (Color)ColorConverter.ConvertFromString(value);

    private void UseThemeColorsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (UseThemeColorsCheckBox.IsChecked != true)
            return;

        OutgoingColorBox.Text = ThemeService.GetOutgoingBubbleColor(SelectedThemeId);
        IncomingColorBox.Text = ThemeService.GetIncomingBubbleColor(SelectedThemeId);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var outgoing = OutgoingColorBox.Text.Trim();
        var incoming = IncomingColorBox.Text.Trim();

        if (!IsValidColor(outgoing) || !IsValidColor(incoming))
        {
            MessageBox.Show(
                this,
                "Enter valid hex colors such as #0A84FF.",
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_conversation is not null)
        {
            var useThemeColors = UseThemeColorsCheckBox.IsChecked == true;

            _conversation.ShowReadReceipts = ReadReceiptsCheckBox.IsChecked == true;
            _conversation.ShowTimestamps = TimestampCheckBox.IsChecked == true;
            _conversation.ShowTypingIndicators = TypingCheckBox.IsChecked == true;
            _conversation.UseThemeBubbleColors = useThemeColors;
            _conversation.OutgoingBubbleColor = useThemeColors
                ? ThemeService.GetOutgoingBubbleColor(SelectedThemeId)
                : outgoing;
            _conversation.IncomingBubbleColor = useThemeColors
                ? ThemeService.GetIncomingBubbleColor(SelectedThemeId)
                : incoming;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyTheme(_originalThemeId);
        DialogResult = false;
    }

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

    private static SolidColorBrush ThemeBrush(string key) =>
        Application.Current.Resources[key] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Transparent);
}
