using Chat.Models;

namespace Chat.Services;

public static class AiResponseService
{
    public static string Generate(ChatCharacter character, ChatMessage trigger)
    {
        var text = trigger.Text.Trim();
        if (text.Length == 0)
            return "Tell me a little more.";

        var lower = text.ToLowerInvariant();

        if (lower.Contains("hello") || lower.Contains("hi ") || lower == "hi")
            return "Hey! Thanks for reaching out. What would you like to talk about?";

        if (lower.Contains("interview") || lower.Contains("job"))
            return "Sure — let's practice. Start by telling me a little about yourself.";

        if (lower.Contains("sorry"))
            return "It's okay. Take your time. What would you like to say next?";

        if (lower.Contains("thanks") || lower.Contains("thank you"))
            return "You're welcome. Let's keep going.";

        if (lower.Contains("?"))
            return "Good question. Take a moment, think it through, and answer naturally.";

        var prefix = character.Role.Length > 0
            ? $"As your {character.Role.ToLowerInvariant()}, "
            : "";

        return $"{prefix}I hear you. Let's keep the conversation going and see where it leads.";
    }
}