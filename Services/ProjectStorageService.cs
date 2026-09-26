using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FakeChatStudio.Models;

namespace FakeChatStudio.Services;

public class ProjectStorageService
{
    private const int CurrentSchemaVersion = 1;

    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FakeChatStudio");

    public string ProjectPath => Path.Combine(_folder, "project.json");

    public void Save(ChatProject project)
    {
        Directory.CreateDirectory(_folder);
        project.SchemaVersion = CurrentSchemaVersion;

        var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
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

            // One-time migration for the old sample project.
            // Older project files had no schema version.
            if (project.SchemaVersion < CurrentSchemaVersion && IsLegacySampleProject(project))
            {
                var emptyProject = CreateEmptyProject();
                Save(emptyProject);
                return emptyProject;
            }

            project.SchemaVersion = CurrentSchemaVersion;
            return project;
        }
        catch
        {
            return CreateEmptyProject();
        }
    }

    private static bool IsLegacySampleProject(ChatProject project)
    {
        var names = project.Characters
            .Select(c => c.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return names.Contains("Alex")
            && names.Contains("Sara")
            && names.Contains("Mike")
            && project.Messages.Count > 0;
    }

    private static ChatProject CreateEmptyProject()
    {
        return new ChatProject
        {
            SchemaVersion = CurrentSchemaVersion,
            Name = "Untitled Project",
            Characters = [],
            Messages = []
        };
    }
}