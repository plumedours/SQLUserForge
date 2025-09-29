using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SQLUserForge.Services
{
    public static class TranslationProvider
    {
        public static Dictionary<string, string> Translations { get; private set; } = new();
        public static string LangFolder { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");
        public static string ConfigPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public static string CurrentLang { get; private set; } = "fr";

        public static event EventHandler? LanguageChanged;

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(LangFolder))
                    Directory.CreateDirectory(LangFolder);

                if (File.Exists(ConfigPath))
                {
                    var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ConfigPath))
                              ?? new Dictionary<string, string>();
                    if (cfg.TryGetValue("language", out var lang) && !string.IsNullOrWhiteSpace(lang))
                        CurrentLang = lang;
                }
                else
                {
                    SaveConfig("fr");
                }
            }
            catch
            {
                // en cas d'erreur, retomber sur fr
                CurrentLang = "fr";
            }

            Load(CurrentLang);
        }

        public static void SetLanguage(string langCode)
        {
            if (string.Equals(CurrentLang, langCode, StringComparison.OrdinalIgnoreCase))
                return;

            Load(langCode);
            SaveConfig(langCode);
        }

        private static void SaveConfig(string langCode)
        {
            CurrentLang = langCode;
            var json = JsonSerializer.Serialize(new Dictionary<string, string> { ["language"] = langCode }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static void Load(string langCode)
        {
            CurrentLang = langCode;
            string path = Path.Combine(LangFolder, $"{langCode}.json");

            if (!File.Exists(path))
            {
                Translations = new();
                LanguageChanged?.Invoke(null, EventArgs.Empty);
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                Translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            catch
            {
                Translations = new();
            }

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string T(string key)
        {
            if (Translations.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                return v;
            return key; // fallback lisible
        }
    }
}
