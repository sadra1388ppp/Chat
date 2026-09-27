namespace Chat.Models;

public sealed class ChatConversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New conversation";
    public string Scenario { get; set; } = "";
    public List<string> ParticipantIds { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
    public bool IsAiEnabled { get; set; }
    public bool IsGroup => ParticipantIds.Count > 1;
    public string PerspectiveId { get; set; } = "self";
    public bool ShowTimestamps { get; set; } = true;
    public bool ShowTypingIndicators { get; set; } = true;
    public bool ShowReadReceipts { get; set; } = true;
    public string OutgoingBubbleColor { get; set; } = "#0A84FF";
    public string IncomingBubbleColor { get; set; } = "#E9EDF2";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}