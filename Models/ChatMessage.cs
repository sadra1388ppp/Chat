namespace Chat.Models;

public enum ChatMessageKind
{
    Text,
    Sticker,
    System
}

public sealed class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SenderId { get; set; } = "self";
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = true;
    public string? Reaction { get; set; }
}