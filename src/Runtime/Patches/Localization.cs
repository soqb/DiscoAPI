using System.Collections.Generic;
using HarmonyLib;
using Il2CppSystem;
using LocalizationCustomSystem;
using Sunshine.Journal;

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

    /// <summary>
    /// Generates a random term and sets the default translation. English is considered the default/fallback language.
    /// If you want to provide your own term or set a specific language translation, use <see cref="SetTermTranslation"/>
    /// </summary>
    public static string CreateTerm(string translation, string lang = "English")
    {
        string fakeTerm = Guid.NewGuid().ToString();
        SetTermTranslation(fakeTerm, translation, lang);
        return fakeTerm;
    }

    /// <summary>
    /// Set the translation for a term. Assign the same term to an object's LocalizationString field for it to be used.
    /// </summary>
    public static void SetTermTranslation(string term, string translation, string lang = "English")
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

    [HarmonyPatch(typeof(JournalTask), nameof(JournalTask.AddSubtask))]
    [HarmonyPatch(typeof(JournalModel), nameof(JournalModel.AddTask))]
    [HarmonyPostfix]
    private static void OnAddTask(ref Completeable __result, string name, string description)
    {
        __result.LocalizedNameTerm = Rosetta.CreateTerm(name);
        __result.LocalizedDescriptionTerm = Rosetta.CreateTerm(description);
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
            else if (TranslationCache["English"].TryGetValue(term, out string? fallback))
            {
                return toUpper ? fallback.ToUpper() : fallback;
            }
        }
        else
        {
            DiscoRunner.Log.LogWarning($"Localization: Asked to localize for language {lang} but it was not found in I2 or Mod Languages! Valid languages: {string.Join(',', I2.Loc.LocalizationManager.GetAllLanguages(true))}");
        }
        return null;
    }

}