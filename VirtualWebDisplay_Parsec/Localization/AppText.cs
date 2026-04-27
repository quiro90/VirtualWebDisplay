using System.Globalization;
using System.Resources;

namespace VirtualWebDisplay.Localization;

public sealed record LanguageOption(string Code, string DisplayName);

public static class AppText
{
    private const string DefaultLanguage = "en";
    private static readonly ResourceManager ResourceManager = new("VirtualWebDisplay.Localization.AppText", typeof(AppText).Assembly);

    private static readonly IReadOnlyList<LanguageOption> SupportedLanguagesInternal =
    [
        new("en", "English"),
        new("es", "Español"),
    ];

    public static IReadOnlyList<LanguageOption> SupportedLanguages => SupportedLanguagesInternal;

    public static string NormalizeLanguage(string? languageCode)
    {
        var normalized = languageCode?.Trim().ToLowerInvariant();
        return SupportedLanguagesInternal.Any(language => language.Code == normalized)
            ? normalized!
            : DefaultLanguage;
    }

    public static void ApplyCulture(string? languageCode)
    {
        var language = NormalizeLanguage(languageCode);
        var culture = CultureInfo.GetCultureInfo(language);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static string Get(string key)
    {
        var value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    public static string HtmlLang => NormalizeLanguage(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
}
