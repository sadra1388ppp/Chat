namespace Chat.Models;

public sealed class ChatCharacter
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Context { get; set; } = "";
    public string Initial { get; set; } = "";
    public string AvatarColor { get; set; } = "#0A84FF";
    public string BubbleColor { get; set; } = "#E9EDF2";
    public bool IsAi { get; set; }
}