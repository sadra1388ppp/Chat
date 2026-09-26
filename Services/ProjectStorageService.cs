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
            return CreateDefault();

        try
        {
            var json = File.ReadAllText(ProjectPath);
            return JsonSerializer.Deserialize<ChatProject>(json) ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public ChatProject CreateDefault()
    {
        return new ChatProject
        {
            Characters =
            [
                new ChatCharacter { Name = "Alex", Role = "Main character", Initial = "A", Color = "#5D5FEF" },
                new ChatCharacter { Name = "Sara", Role = "Friend", Initial = "S", Color = "#E56B9A" },
                new ChatCharacter { Name = "Mike", Role = "Friend", Initial = "M", Color = "#E7A84B" }
            ],
            Messages =
            [
                new ChatMessage { Sender = "Alex", Text = "Hey! Are you still working on the story?", Time = "18:30" },
                new ChatMessage { Sender = "Sara", Text = "Yeah 😄 I just finished the first scene.", Time = "18:31" },
                new ChatMessage { Sender = "Alex", Text = "Nice. Send it to me when you're ready.", Time = "18:32" },
                new ChatMessage { Sender = "Sara", Text = "Give me two minutes.", Time = "18:33" }
            ]
        };
    }
}