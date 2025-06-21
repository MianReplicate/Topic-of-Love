using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;

namespace Topic_of_Love.Mian;

// Credits to NML for their MIT License! :heart:
public class LM
{
    private static Dictionary<string, Dictionary<string, string>> _locales = new()
    {
        ["en"]={}
    };

    public static void LoadLocales()
    {
        var localeFiles = Directory.GetFiles(TopicOfLove.GetModDirectory() + "\\Locales");

        foreach (var langPath in localeFiles)
        {
            var language = Path.GetFileNameWithoutExtension(langPath);
            var locale = JsonConvert.DeserializeObject<Dictionary<string, string>>(new StreamReader(langPath).ReadToEnd());
            _locales.Add(language, locale);
        }
        ApplyLocale();
    }
    private static void ApplyLocale()
    {
        ApplyLocale(LocalizedTextManager.instance.language);
    }
    private static void ApplyLocale(string language)
    {
        if(!_locales.ContainsKey(language))
            _locales.Add(language, new Dictionary<string, string>());

        foreach (var (key, value) in _locales[language].Select(pair => (pair.Key, pair.Value)))
        {
            LocalizedTextManager.instance._localized_text[key] = value;
        }
            
        // english is default
        foreach (var (key, value) in _locales["en"].Where(pair => !LocalizedTextManager.instance._localized_text.ContainsKey(pair.Key))
                     .Select(pair => (pair.Key, pair.Value)))
        {
            LocalizedTextManager.instance._localized_text[key] = value;
        }
            
        LocalizedTextManager.updateTexts();
    }
    
    // adds to default
    public static void AddToCore(string key, string value)
    {
        _locales["en"][key] = value;
        if(!LocalizedTextManager.instance._localized_text.ContainsKey(key))
            LocalizedTextManager.instance._localized_text[key] = value;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocalizedTextManager), nameof(LocalizedTextManager.setLanguage))]
    private static void SetLanguagePostfix(string pLanguage)
    {
        ApplyLocale(pLanguage);
    }
}