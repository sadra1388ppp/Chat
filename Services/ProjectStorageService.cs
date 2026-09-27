using System.IO;
using System.Text.Json;
using Chat.Models;

namespace Chat.Services;

public sealed class ProjectStorageService
{
    private const int CurrentSchemaVersion = 4;

    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Chat");

    public string ProjectPath => Path.Combine(_folder, "project.json");

    public void Save(ChatProject project)
    {
        Directory.CreateDirectory(_folder);
        project.SchemaVersion = CurrentSchemaVersion;

        var json = JsonSerializer.Serialize(project, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(ProjectPath, json);
    }

    public ChatProject Load()
    {
        if (!File.Exists(ProjectPath))
            return CreateEmptyProject();

        try
        {
            var json = File.ReadAllText(ProjectPath);
            var project = JsonSerializer.Deserialize<ChatProject>(json);

            if (project is null)
                return CreateEmptyProject();

            project.Contacts ??= [];
            project.Conversations ??= [];
            project.ThemeId = ThemeService.Normalize(project.ThemeId);

            foreach (var contact in project.Contacts)
            {
                contact.Initial = string.IsNullOrWhiteSpace(contact.Initial)
                    ? BuildInitial(contact.Name)
                    : contact.Initial;
            }

            return project;
        }
        catch
        {
            return CreateEmptyProject();
        }
    }

    private static ChatProject CreateEmptyProject() => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        Name = "Chat",
        ThemeId = ThemeService.Light,
        CurrentUserName = "You",
        Contacts = [],
        Conversations = [],
        ActiveConversationId = null
    };

    private static string BuildInitial(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        return trimmed.Length == 0 ? "?" : trimmed[..1].ToUpperInvariant();
    }
}