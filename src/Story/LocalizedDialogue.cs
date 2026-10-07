using System;
using System.Collections.Generic;
using System.Text;

namespace Evolutionist.Story;

// 一条按游戏语言选择的中英文剧情对白。
internal readonly struct LocalizedDialogueLine
{
    public LocalizedDialogueLine(string chinese, string english)
    {
        Chinese = chinese;
        English = english;
    }

    public string Chinese { get; }

    public string English { get; }
}

// 按当前字体宽度将剧情对白限制为每条消息最多两行。
internal static class LocalizedDialogue
{
    private const int MaximumLinesPerMessage = 2;

    public static void AddLines(
        Conversation conversation,
        IReadOnlyList<LocalizedDialogueLine> sourceLines)
    {
        HUD.DialogBox dialogBox = conversation.dialogBox;
        bool useChinese = IsChinese(dialogBox.hud.rainWorld.inGameTranslator.currentLanguage);
        float maximumLineWidth = Math.Min(
            880f,
            Math.Max(500f, dialogBox.hud.rainWorld.screenSize.x - 240f));

        foreach (LocalizedDialogueLine sourceLine in sourceLines)
        {
            string text = useChinese ? sourceLine.Chinese : sourceLine.English;
            List<string> wrappedLines = Wrap(dialogBox.label, text, maximumLineWidth, useChinese);

            for (int i = 0; i < wrappedLines.Count; i += MaximumLinesPerMessage)
            {
                string page = wrappedLines[i];
                if (i + 1 < wrappedLines.Count)
                {
                    page += "<LINE>" + wrappedLines[i + 1];
                }

                conversation.events.Add(new Conversation.TextEvent(
                    conversation,
                    0,
                    page,
                    GetLinger(page)));
            }
        }
    }

    public static string Resolve(
        HUD.DialogBox dialogBox,
        LocalizedDialogueLine line)
    {
        return IsChinese(dialogBox.hud.rainWorld.inGameTranslator.currentLanguage)
            ? line.Chinese
            : line.English;
    }

    private static bool IsChinese(InGameTranslator.LanguageID language)
    {
        return language == InGameTranslator.LanguageID.Chinese ||
            language == InGameTranslator.LanguageID.TraditionalChinese;
    }

    private static List<string> Wrap(
        FLabel label,
        string text,
        float maximumLineWidth,
        bool useChinese)
    {
        var result = new List<string>();
        string[] requestedLines = text.Split(
            new[] { "<LINE>" },
            StringSplitOptions.None);

        foreach (string requestedLine in requestedLines)
        {
            if (useChinese)
            {
                WrapChineseLine(label, requestedLine, maximumLineWidth, result);
            }
            else
            {
                WrapEnglishLine(label, requestedLine, maximumLineWidth, result);
            }
        }

        if (result.Count == 0)
        {
            result.Add(string.Empty);
        }

        return result;
    }

    private static void WrapChineseLine(
        FLabel label,
        string text,
        float maximumLineWidth,
        List<string> result)
    {
        var current = new StringBuilder();
        foreach (char character in text)
        {
            string candidate = current.ToString() + character;
            if (current.Length > 0 && Measure(label, candidate) > maximumLineWidth)
            {
                result.Add(current.ToString());
                current.Clear();
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }
    }

    private static void WrapEnglishLine(
        FLabel label,
        string text,
        float maximumLineWidth,
        List<string> result)
    {
        string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (string word in words)
        {
            string candidate = current.Length == 0
                ? word
                : current + " " + word;

            if (current.Length > 0 && Measure(label, candidate) > maximumLineWidth)
            {
                result.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }
    }

    private static float Measure(FLabel label, string text)
    {
        string originalText = label.text;
        try
        {
            label.text = text;
            return label.textRect.width;
        }
        finally
        {
            label.text = originalText;
        }
    }

    private static int GetLinger(string text)
    {
        int visibleLength = text.Replace("<LINE>", string.Empty).Length;
        return Math.Min(180, 70 + visibleLength * 2);
    }
}
