namespace Evolutionist.Story;

// 写入剧情存档的稳定结局标识。
internal static class StoryEndingType
{
    public const string None = "None";
    public const string UnknowingAscension = "UnknowingAscension";
    public const string ObedientAscension = "ObedientAscension";
    public const string ChosenAscension = "ChosenAscension";
    public const string MessengerWithoutMessage = "MessengerWithoutMessage";

    public static bool IsEnding(string? value)
    {
        return value == UnknowingAscension ||
               value == ObedientAscension ||
               value == ChosenAscension ||
               value == MessengerWithoutMessage;
    }

    public static string Normalize(string? value)
    {
        return IsEnding(value) ? value! : None;
    }
}
