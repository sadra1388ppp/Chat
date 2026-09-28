using Chat.Models;

namespace Chat.Services;

public static class AiResponseService
{
    public static string Generate(
        ChatCharacter character,
        ChatMessage trigger,
        string scenario,
        IReadOnlyList<ChatMessage> history)
    {
        var text = trigger.Text.Trim();

        if (text.Length == 0)
            return "Tell me a little more.";

        var lower = text.ToLowerInvariant();
        var scenarioText = scenario.Trim().ToLowerInvariant();
        var isPersian = ContainsPersian(text);

        if (isPersian)
        {
            if (ContainsAny(lower, "سلام", "درود", "هی", "خوبی"))
                return "سلام! من آماده‌ام. دوست داری از کجا شروع کنیم؟";

            if (ContainsAny(lower, "ممنون", "مرسی", "متشکرم"))
                return "خواهش می‌کنم. ادامه بده، با دقت گوش می‌دم.";

            if (ContainsAny(lower, "ببخشید", "معذرت"))
                return "اشکالی نداره. با آرامش ادامه بده؛ دوست داری دقیقاً چی بگی؟";

            if (ContainsAny(lower, "مصاحبه", "شغل", "استخدام") ||
                ContainsAny(scenarioText, "interview", "job"))
                return "خوبه؛ برای تمرین مصاحبه، جواب کوتاه و مشخص بده. درباره تجربه و مهارت اصلیت بگو.";

            if (ContainsAny(lower, "پشتیبانی", "سفارش", "خرید", "مشکل"))
                return "متوجه شدم. اول مشکل را در یک جمله مشخص کنیم. شماره سفارش یا جزئیات اصلی را بگو.";

            if (ContainsAny(lower, "دعوا", "اختلاف", "ناراحت", "سخت"))
                return "می‌فهمم. سعی کنیم بدون سرزنش جلو بریم. دقیقاً چه نتیجه‌ای می‌خوای از این گفتگو بگیری؟";

            if (text.Contains('?'))
                return "سؤال خوبیه. جواب کوتاه و طبیعی بده و یک دلیل یا مثال کوچک هم اضافه کن.";

            return (history.Count % 3) switch
            {
                0 => "ادامه بده؛ من گوش می‌دم. نکته بعدی که می‌خوای بگی چیه؟",
                1 => "متوجه شدم. یکم بیشتر توضیح بده تا بهتر بتونیم این موقعیت رو تمرین کنیم.",
                _ => "خوبه. حالا فرض کنیم من طرف مقابل هستم؛ قدم بعدی حرفت رو بگو."
            };
        }

        if (ContainsAny(lower, "hello", "hi", "hey", "good morning", "good evening"))
            return "Hey! I'm ready. What would you like to practice?";

        if (ContainsAny(lower, "sorry", "apologize", "apologies"))
            return "That's okay. Take a breath and continue. What would you like to say next?";

        if (ContainsAny(lower, "thanks", "thank you"))
            return "You're welcome. Keep going — I'll stay in character.";

        if (ContainsAny(lower, "interview", "job", "hiring", "resume") ||
            ContainsAny(scenarioText, "interview", "job"))
        {
            if (ContainsAny(lower, "experience", "worked", "background"))
                return "Good. Give me one specific example, what you did, and what the result was.";

            if (ContainsAny(lower, "strength"))
                return "Pick one strength that fits the role, then support it with a short real example.";

            if (ContainsAny(lower, "weakness"))
                return "Choose a genuine weakness that you're actively improving, and explain what you're doing about it.";

            return "Good — let's make this realistic. Tell me about yourself in about 30 seconds.";
        }

        if (ContainsAny(lower, "support", "order", "refund", "delivery", "customer"))
        {
            if (ContainsAny(lower, "refund", "money back"))
                return "I can help with that. Tell me what happened and what outcome you expect.";

            return "I understand the issue. Give me the key details first, and I'll respond as the support representative.";
        }

        if (ContainsAny(lower, "difficult", "argument", "upset", "angry", "disagree"))
            return "I hear you. Let's keep this constructive. What outcome would you like from this conversation?";

        if (text.Contains('?'))
            return "Good question. Answer naturally, then add one concrete detail so the conversation can move forward.";

        return history.Count % 3 switch
        {
            0 => "I hear you. Keep going — what's the next thing you'd say?",
            1 => "That makes sense. Tell me a little more so I can respond realistically.",
            _ => "Got it. Let's continue from there. What would you say next?"
        };
    }

    private static bool ContainsPersian(string text) =>
        text.Any(c => c is >= '\u0600' and <= '\u06FF');

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(value.Contains);
}
