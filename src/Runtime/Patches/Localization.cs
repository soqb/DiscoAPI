using System.Collections.Generic;
using HarmonyLib;
using Il2CppSystem;
using LocalizationCustomSystem;

namespace DiscoAPI.Runtime.Patches;

public static class Rosetta
{
    private static Dictionary<string, Dictionary<string, string>> TranslationCache = new();

    internal static void SetupLangCaches()
    {
        var langs = I2.Loc.LocalizationManager.GetAllLanguages(true);
        foreach (var lang in langs)
        {
            if (lang == null) continue;
            TranslationCache.TryAdd(lang, new Dictionary<string, string>());
        }
    }

    public static string CreateLangAgnosticTerm(string value)
    {
        string fakeTerm = Guid.NewGuid().ToString();
        foreach (var (_, terms) in TranslationCache)
        {
            terms.TryAdd(fakeTerm, value);
        }
        return fakeTerm;
    }

    public static void SetTermTranslation(string term, string translation, string lang)
    {
        if (TranslationCache.TryGetValue(lang, out var langMap))
        {
            langMap[term] = translation;
        }
        else
        {
            DiscoRunner.Log.LogWarning($"Localization: Could not set term {term} for language {lang} -- language does not exist! Valid languages: {string.Join(',', I2.Loc.LocalizationManager.GetAllLanguages(true))}");
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetLocalizedTerm), typeof(string), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    private static bool OnGetTransString(string term, ref string __result)
    {
        string? lang = I2.Loc.LocalizationManager.CurrentLanguage;
        string? result = GetTranslation(term, false, lang);
        if (result == null) return true;
        __result = result;
        return false;
    }

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetLocalizedTerm), typeof(string), typeof(string))]
    [HarmonyPrefix]
    private static bool OnGetTransStringWithLang(string term, string languageName, ref string __result)
    {
        string? result = GetTranslation(term, false, languageName);
        if (result == null) return true;
        __result = result;
        return false;
    }

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetLocalizedTermToUpper))]
    [HarmonyPrefix]
    private static bool OnGetTransStringUpper(string term, ref string __result)
    {
        string? lang = I2.Loc.LocalizationManager.CurrentLanguage;
        string? result = GetTranslation(term, true, lang);
        if (result == null) return true;
        __result = result;
        return false;
    }

    /// <summary>
    /// Only fetches translations for mod terms. Prefer using LocalizationManager.GetLocalizedTerm() instead.
    /// </summary>
    public static string? GetTranslation(string? term, bool toUpper = false, string lang = "English")
    {
        if (term != null && TranslationCache.TryGetValue(lang, out var langMap))
        {
            if (langMap.TryGetValue(term, out string? value))
            {
                return toUpper ? value.ToUpper() : value;
            }
        }
        else
        {
            DiscoRunner.Log.LogWarning($"Localization: Asked to localize for language {lang} but it was not found in I2 or Mod Languages! Valid languages: {string.Join(',', I2.Loc.LocalizationManager.GetAllLanguages(true))}");
        }
        return null;
    }

}