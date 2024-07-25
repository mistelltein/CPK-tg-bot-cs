namespace CPK_Bot.Helpers;

public static class TelegramMarkdownHelper
{
    public static string EscapeMarkdownV2(string text)
    {
        var escapeChars = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        return escapeChars.Aggregate(text, (current, ch) => current.Replace(ch, "\\" + ch));
    }
}