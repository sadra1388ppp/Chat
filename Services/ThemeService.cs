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
    public const string Sunset = "sunset";
    public const string Graphite = "graphite";
    public const string Arctic = "arctic";
    public const string Plum = "plum";

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
                "#E45D83", "#F1E4E8", "#C93655", "#301A22", "#40232D", "#BDA4AD"),

            [Sunset] = new(
                "#FFF6F0", "#FFFFFF", "#FBE9DE", "#F0D6C7", "#342117", "#806A5F",
                "#F26A3D", "#D84B20", "#FFE7DC", "#FBE9DE", "#FFF0E8",
                "#F26A3D", "#F1DED3", "#C84352", "#2A1813", "#3A2119", "#C7A69A"),

            [Graphite] = new(
                "#15171A", "#1D2024", "#272B30", "#343A41", "#F1F3F5", "#9FA7B2",
                "#8C9AA8", "#6F7F8F", "#25313C", "#272B30", "#26303A",
                "#6D8EA8", "#2A3036", "#FF6B7D", "#111316", "#1B1E22", "#9FA7B2"),

            [Arctic] = new(
                "#F1F7FC", "#FFFFFF", "#E5F0F9", "#D2E1EE", "#172B3A", "#62788A",
                "#2D8FD5", "#1F70AA", "#E2F1FC", "#EAF3FA", "#E4F2FC",
                "#2D8FD5", "#E5EDF4", "#D94C64", "#12202C", "#192A37", "#9EB2C1"),

            [Plum] = new(
                "#F8F2F9", "#FFFFFF", "#F0E5F2", "#E3D5E7", "#332037", "#7B697E",
                "#A05AE8", "#7E3DC2", "#F0E4FF", "#F0E5F2", "#F2E8FF",
                "#A05AE8", "#EDE5F0", "#D54E75", "#28192D", "#36203C", "#AE9DB4")
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
            Sunset => Palettes[Sunset].MessageOutgoing,
            Graphite => Palettes[Graphite].MessageOutgoing,
            Arctic => Palettes[Arctic].MessageOutgoing,
            Plum => Palettes[Plum].MessageOutgoing,
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
            Sunset => Palettes[Sunset].MessageIncoming,
            Graphite => Palettes[Graphite].MessageIncoming,
            Arctic => Palettes[Arctic].MessageIncoming,
            Plum => Palettes[Plum].MessageIncoming,
            _ => Palettes[Light].MessageIncoming
        };

    public static string GetParticipantBubbleColor(string? themeId, int index)
    {
        var colors = Normalize(themeId) switch
        {
            Dark => new[] { "#5D8CFF", "#A56BFF", "#E58A4F", "#42BE8C", "#D86BA5", "#39A8C7", "#D0A33A", "#5C78C9", "#C45C5C", "#4B9ED1", "#9B67C0", "#559C63" },
            Midnight => new[] { "#607DFF", "#A76FFF", "#F08E58", "#43C7A0", "#E06BAA", "#4AAFC8", "#D6AE3B", "#647CD0", "#D05B67", "#4AA5D6", "#A56CCB", "#5CAA6B" },
            Ocean => new[] { "#008BA7", "#7559D9", "#D96D42", "#279B70", "#C65D8A", "#438AA5", "#BE941E", "#4771C0", "#C65B5B", "#398FB6", "#8E5AC2", "#4E986B" },
            Lavender => new[] { "#7C5CFC", "#B04CDB", "#E07355", "#2BAA78", "#D45A91", "#4B91C7", "#B89424", "#5D74C8", "#C5535E", "#438FAF", "#915DB8", "#4B9661" },
            Mint => new[] { "#2B8F70", "#7D62D9", "#D66F4C", "#238E63", "#C95D8B", "#3A8FA8", "#B49327", "#5974C5", "#C2535F", "#3E8FAE", "#925DB8", "#4A9561" },
            Rose => new[] { "#C84C73", "#7656D7", "#D46A45", "#268F6B", "#B34F8D", "#3D8EAA", "#B98F22", "#5B74C4", "#C4545E", "#3D8FB0", "#8F5DB6", "#4E9863" },
            Sunset => new[] { "#D9542E", "#735AD2", "#C97932", "#238C69", "#BB4F87", "#378BA4", "#B78E22", "#5D73C5", "#C65458", "#418EAE", "#8E5DB7", "#4D9663" },
            Graphite => new[] { "#6D8EA8", "#9B72D8", "#D07C4C", "#49A986", "#C56D9A", "#4E9DB5", "#C09A36", "#667FC6", "#CF686C", "#4B9BB8", "#9468BD", "#579B68" },
            Arctic => new[] { "#2D7FBD", "#795FD2", "#D37646", "#2A956F", "#C35F8C", "#3C90AA", "#B99425", "#5574C7", "#C2545D", "#3E90B0", "#925DBA", "#4C9860" },
            Plum => new[] { "#8D4ED0", "#C458A4", "#D06C46", "#2B956F", "#B94F83", "#3B8EA8", "#B68E25", "#5E71C5", "#C7545E", "#3F90AF", "#8E5FB8", "#4D9865" },
            _ => new[] { "#4D74D9", "#7D5AD8", "#C96D3E", "#28966D", "#C35687", "#298DA8", "#B68F25", "#5A74C6", "#C5555E", "#3E91B0", "#8E5CB9", "#4D9864" }
        };

        return colors[Math.Abs(index) % colors.Length];
    }

    private static void SetBrush(string key, string color)
    {
        Application.Current.Resources[key] = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(color));
    }
}
