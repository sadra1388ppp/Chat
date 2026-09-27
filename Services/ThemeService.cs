using System.Windows;
using System.Windows.Media;

namespace Chat.Services;

public static class ThemeService
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string Midnight = "midnight";
    public const string Ocean = "ocean";
    public const string Lavender = "lavender";
    public const string Mint = "mint";
    public const string Rose = "rose";

    private sealed record ThemePalette(
        string WindowBackground,
        string PanelBackground,
        string SoftPanel,
        string Divider,
        string TextPrimary,
        string TextSecondary,
        string Accent,
        string AccentDark,
        string AccentSoft,
        string InputBackground,
        string SelectedBackground,
        string MessageOutgoing,
        string MessageIncoming,
        string Danger,
        string CallBackground,
        string CallPanel,
        string CallSecondary);

    private static readonly IReadOnlyDictionary<string, ThemePalette> Palettes =
        new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
        {
            [Light] = new(
                "#F4F6F8", "#FFFFFF", "#F1F2F6", "#E3E7EC", "#111827", "#667085",
                "#0A84FF", "#0066CC", "#E8F1FF", "#F1F2F6", "#EBF5FF",
                "#0A84FF", "#E9EDF2", "#D92D20", "#101418", "#171C22", "#A9B3BF"),

            [Dark] = new(
                "#0F1115", "#171A21", "#20242C", "#2A303A", "#F5F7FA", "#A7AFBD",
                "#4DA3FF", "#2C7FD6", "#163451", "#20242C", "#1B2738",
                "#2F8FFF", "#262B33", "#FF6B7D", "#0F1115", "#171A21", "#A7AFBD"),

            [Midnight] = new(
                "#0B1020", "#12182A", "#18213A", "#28324D", "#F3F6FF", "#A6B1C8",
                "#7C9CFF", "#5A7DE6", "#1B2A54", "#18213A", "#1A2542",
                "#536DFF", "#222C44", "#FF6B8A", "#090E1C", "#12182A", "#A6B1C8"),

            [Ocean] = new(
                "#EEF7FA", "#FFFFFF", "#E4F2F5", "#D4E5E9", "#102A33", "#60777E",
                "#00A6C7", "#007C96", "#DFF7FB", "#E8F3F5", "#E2F8FC",
                "#00A6C7", "#E3F1F3", "#D94C64", "#102A33", "#173842", "#9BB6BD"),

            [Lavender] = new(
                "#F7F4FB", "#FFFFFF", "#F0EBF8", "#E5DEF0", "#2A2234", "#786C86",
                "#7C5CFC", "#6242E2", "#EEE8FF", "#F1ECF8", "#F0EAFF",
                "#7C5CFC", "#ECE8F5", "#D94C70", "#241D30", "#30273D", "#B8ACBF"),

            [Mint] = new(
                "#F1F8F4", "#FFFFFF", "#E8F3ED", "#D7E7DD", "#163026", "#63786F",
                "#2BAA78", "#21865E", "#E2F6EE", "#E8F3ED", "#E2F6EE",
                "#2BAA78", "#E5F0EB", "#D94C64", "#12241E", "#183128", "#A7BFB4"),

            [Rose] = new(
                "#FCF4F7", "#FFFFFF", "#F8EAEE", "#F0DCE2", "#341E26", "#806A73",
                "#E45D83", "#C84468", "#FCE7EF", "#F8EAEE", "#FCEAF0",
                "#E45D83", "#F1E4E8", "#C93655", "#301A22", "#40232D", "#BDA4AD")
        };

    public static string CurrentThemeId { get; private set; } = Light;

    public static string Normalize(string? themeId) =>
        !string.IsNullOrWhiteSpace(themeId) && Palettes.ContainsKey(themeId)
            ? themeId
            : Light;

    public static void ApplyTheme(string? themeId)
    {
        var normalized = Normalize(themeId);
        var palette = Palettes[normalized];
        CurrentThemeId = normalized;

        SetBrush("WindowBackground", palette.WindowBackground);
        SetBrush("PanelBackground", palette.PanelBackground);
        SetBrush("SoftPanel", palette.SoftPanel);
        SetBrush("Divider", palette.Divider);
        SetBrush("TextPrimary", palette.TextPrimary);
        SetBrush("TextSecondary", palette.TextSecondary);
        SetBrush("Accent", palette.Accent);
        SetBrush("AccentDark", palette.AccentDark);
        SetBrush("AccentSoft", palette.AccentSoft);
        SetBrush("InputBackground", palette.InputBackground);
        SetBrush("SelectedBackground", palette.SelectedBackground);
        SetBrush("MessageOutgoing", palette.MessageOutgoing);
        SetBrush("MessageIncoming", palette.MessageIncoming);
        SetBrush("Danger", palette.Danger);
        SetBrush("CallBackground", palette.CallBackground);
        SetBrush("CallPanel", palette.CallPanel);
        SetBrush("CallSecondary", palette.CallSecondary);
    }

    public static string GetOutgoingBubbleColor(string? themeId) =>
        Normalize(themeId) switch
        {
            Dark => Palettes[Dark].MessageOutgoing,
            Midnight => Palettes[Midnight].MessageOutgoing,
            Ocean => Palettes[Ocean].MessageOutgoing,
            Lavender => Palettes[Lavender].MessageOutgoing,
            Mint => Palettes[Mint].MessageOutgoing,
            Rose => Palettes[Rose].MessageOutgoing,
            _ => Palettes[Light].MessageOutgoing
        };

    public static string GetIncomingBubbleColor(string? themeId) =>
        Normalize(themeId) switch
        {
            Dark => Palettes[Dark].MessageIncoming,
            Midnight => Palettes[Midnight].MessageIncoming,
            Ocean => Palettes[Ocean].MessageIncoming,
            Lavender => Palettes[Lavender].MessageIncoming,
            Mint => Palettes[Mint].MessageIncoming,
            Rose => Palettes[Rose].MessageIncoming,
            _ => Palettes[Light].MessageIncoming
        };

    private static void SetBrush(string key, string color)
    {
        Application.Current.Resources[key] = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(color));
    }
}
