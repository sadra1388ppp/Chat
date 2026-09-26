namespace FakeChatStudio.Models;

public class ChatProject
{
    public int SchemaVersion { get; set; } = 0;
    public string Name { get; set; } = "Untitled Project";
    public List<ChatCharacter> Characters { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
}