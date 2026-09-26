namespace FakeChatStudio.Models;

public class ChatProject
{
    public string Name { get; set; } = "Midnight Story";
    public List<ChatCharacter> Characters { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
}