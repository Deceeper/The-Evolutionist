namespace Evolutionist;

/// <summary>
/// 为固定中文界面查找并按需加载游戏自带字体。
/// </summary>
internal static class ChineseFontService
{
    public static string? EnsureLoaded()
    {
        string shortCode = LocalizationTranslator.LangShort(InGameTranslator.LanguageID.Chinese);
        string fullFontName = "font" + shortCode + "Full";
        string regularFontName = "font" + shortCode;

        string? loadedFont = FindLoadedFont(fullFontName, regularFontName);
        if (loadedFont != null)
        {
            return loadedFont;
        }

        InGameTranslator.LoadFonts(InGameTranslator.LanguageID.Chinese, null);
        loadedFont = FindLoadedFont(fullFontName, regularFontName);
        if (loadedFont != null)
        {
            return loadedFont;
        }

        // 中文游戏可能由其他模组以不同名称注册字体，此时沿用当前菜单字体。
        string currentFont = RWCustom.Custom.GetFont();
        return Futile.atlasManager.DoesContainFontWithName(currentFont) ? currentFont : null;
    }

    private static string? FindLoadedFont(string fullFontName, string regularFontName)
    {
        if (Futile.atlasManager.DoesContainFontWithName(fullFontName))
        {
            return fullFontName;
        }

        return Futile.atlasManager.DoesContainFontWithName(regularFontName) ? regularFontName : null;
    }
}
