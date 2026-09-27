namespace Chat.Models;

public sealed class ChatProject
{
    public int SchemaVersion { get; set; } = 3;
    public string Name { get; set; } = "Chat";
    public string CurrentUserName { get; set; } = "You";
    public List<ChatCharacter> Contacts { get; set; } = [];
    public List<ChatConversation> Conversations { get; set; } = [];
    public string? ActiveConversationId { get; set; }
}