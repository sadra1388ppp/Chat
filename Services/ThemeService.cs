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
    public const string Telegram = "telegram";
    public const string Instagram = "instagram";
    public const string Forest = "forest";
    public const string Coffee = "coffee";
    public const string Neon = "neon";
    public const string Sakura = "sakura";
    public const string Solar = "solar";
    public const string Slate = "slate";

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
                "#A05AE8", "#EDE5F0", "#D54E75", "#28192D", "#36203C", "#AE9DB4"),

            [Telegram] = new(
                "#F2F7FA", "#FFFFFF", "#E6F0F5", "#D5E2E8", "#17232D", "#687A86",
                "#229ED9", "#1684B8", "#DDF3FF", "#EAF3F7", "#E0F3FF",
                "#229ED9", "#E8F1F5", "#D6455D", "#10202A", "#172C38", "#A7BBC5"),

            [Instagram] = new(
                "#FFF4F9", "#FFFFFF", "#FBE4EF", "#EED4E0", "#341B2A", "#7D6873",
                "#E1306C", "#C2185B", "#FFE0EC", "#F9E9F1", "#FFE7F0",
                "#E1306C", "#F3E5EC", "#C93655", "#2B1722", "#3A1E2D", "#BEA4B0"),

            [Forest] = new(
                "#F1F7F3", "#FFFFFF", "#E3EFE7", "#D2E3D8", "#193025", "#63766C",
                "#2F8A5B", "#246B46", "#DDF3E6", "#E8F2EB", "#DFF4E7",
                "#2F8A5B", "#E2EEE7", "#C94356", "#12261B", "#193429", "#A7BEB0"),

            [Coffee] = new(
                "#F8F3EE", "#FFFFFF", "#EFE6DD", "#E2D6C9", "#34271F", "#7B6D63",
                "#A96D3D", "#85532D", "#F7E5D2", "#F1E9E1", "#F4E2D2",
                "#A96D3D", "#EEE4DB", "#C84C4C", "#261A14", "#352319", "#B9A79A"),

            [Neon] = new(
                "#0B0F14", "#111820", "#19232D", "#27333E", "#F4F8FC", "#9FAFBE",
                "#00E5FF", "#00B8CC", "#103942", "#18232D", "#12323B",
                "#00D6F0", "#26333D", "#FF5876", "#071016", "#101820", "#A9BBC7"),

            [Sakura] = new(
                "#FFF5F7", "#FFFFFF", "#FCE7EC", "#F0D4DA", "#351D25", "#806C73",
                "#F06F91", "#D9577A", "#FFE4EB", "#FAEAF0", "#FFE7EE",
                "#F06F91", "#F4E5E9", "#C83B54", "#2E1820", "#3B202A", "#BFA6AE"),

            [Solar] = new(
                "#FFF8EA", "#FFFFFF", "#F9EDD2", "#EEDDB4", "#382814", "#806E50",
                "#E6A400", "#B77D00", "#FFF0C7", "#F8EFD9", "#FFF1C9",
                "#E6A400", "#F1E7CF", "#C94745", "#2A1E0F", "#382914", "#C1AF86"),

            [Slate] = new(
                "#F2F5F8", "#FFFFFF", "#E4EAF0", "#D2DBE4", "#1C2732", "#6C7B88",
                "#5B6F82", "#46596A", "#E6EDF4", "#EAF0F5", "#E8EFF5",
                "#5B6F82", "#E5EBF0", "#D14B5D", "#162029", "#202B34", "#A7B4BF")
        };

    public static string CurrentThemeId { get; private set; } = Light;

    public sealed record ThemeOption(
        string Id,
        string Name,
        string PreviewBackground,
        string PreviewAccent,
        string PreviewBorder,
        string PreviewText);

    public static IReadOnlyList<ThemeOption> GetThemeOptions() =>
    [
        new(Light, "Light", "#F4F6F8", "#0A84FF", "#DDE3EA", "#111827"),
        new(Dark, "Dark", "#0F1115", "#4DA3FF", "#2A303A", "#F5F7FA"),
        new(Midnight, "Midnight", "#0B1020", "#7C9CFF", "#28324D", "#F3F6FF"),
        new(Ocean, "Ocean", "#EEF7FA", "#00A6C7", "#B8E4EA", "#102A33"),
        new(Lavender, "Lavender", "#F7F4FB", "#7C5CFC", "#DCD0F5", "#2A2234"),
        new(Mint, "Mint", "#F1F8F4", "#2BAA78", "#BFE5D1", "#163026"),
        new(Rose, "Rose", "#FCF4F7", "#E45D83", "#F0C7D5", "#341E26"),
        new(Sunset, "Sunset", "#FFF6F0", "#F26A3D", "#F0D6C7", "#342117"),
        new(Graphite, "Graphite", "#15171A", "#8C9AA8", "#343A41", "#F1F3F5"),
        new(Arctic, "Arctic", "#F1F7FC", "#2D8FD5", "#D2E1EE", "#172B3A"),
        new(Plum, "Plum", "#F8F2F9", "#A05AE8", "#E3D5E7", "#332037"),
        new(Telegram, "Telegram", "#F2F7FA", "#229ED9", "#D5E2E8", "#17232D"),
        new(Instagram, "Instagram", "#FFF4F9", "#E1306C", "#EED4E0", "#341B2A"),
        new(Forest, "Forest", "#F1F7F3", "#2F8A5B", "#D2E3D8", "#193025"),
        new(Coffee, "Coffee", "#F8F3EE", "#A96D3D", "#E2D6C9", "#34271F"),
        new(Neon, "Neon", "#0B0F14", "#00E5FF", "#27333E", "#F4F8FC"),
        new(Sakura, "Sakura", "#FFF5F7", "#F06F91", "#F0D4DA", "#351D25"),
        new(Solar, "Solar", "#FFF8EA", "#E6A400", "#EEDDB4", "#382814"),
        new(Slate, "Slate", "#F2F5F8", "#5B6F82", "#D2DBE4", "#1C2732")
    ];

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
        Palettes[Normalize(themeId)].MessageOutgoing;

    public static string GetIncomingBubbleColor(string? themeId) =>
        Palettes[Normalize(themeId)].MessageIncoming;

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
