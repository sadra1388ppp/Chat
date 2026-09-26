using System;
using System.IO;
using System.Text.Json;
using FakeChatStudio.Models;

namespace FakeChatStudio.Services;

public class ProjectStorageService
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FakeChatStudio");

    public string ProjectPath => Path.Combine(_folder, "project.json");

    public void Save(ChatProject project)
    {
        Directory.CreateDirectory(_folder);
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
            return JsonSerializer.Deserialize<ChatProject>(json) ?? CreateEmptyProject();
        }
        catch
        {
            return CreateEmptyProject();
        }
    }

    private static ChatProject CreateEmptyProject()
    {
        return new ChatProject
        {
            Name = "Untitled Project",
            Characters = [],
            Messages = []
        };
    }
}