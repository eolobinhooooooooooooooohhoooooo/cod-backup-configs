using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CodBackupConfig
{
    // tiny json in %LOCALAPPDATA%\CodBackupConfigs\prefs.json — best effort
    static class Prefs
    {
        static string Path => System.IO.Path.Combine(BackupStore.Root, "prefs.json");

        public static string Lang = "en";
        public static string LastGame = "mw4";
        public static Dictionary<string, string> FolderOverride = new();

        public static void Load()
        {
            try
            {
                if (!File.Exists(Path)) return;
                var p = JsonSerializer.Deserialize<PrefFile>(File.ReadAllText(Path));
                if (p == null) return;
                Lang = p.Lang == "pt" ? "pt" : "en";
                if (!string.IsNullOrEmpty(p.LastGame)) LastGame = p.LastGame;
                if (p.Overrides != null) FolderOverride = new Dictionary<string, string>(p.Overrides);
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(BackupStore.Root);
                File.WriteAllText(Path, JsonSerializer.Serialize(new PrefFile
                {
                    Lang = Lang,
                    LastGame = LastGame,
                    Overrides = FolderOverride,
                }));
            }
            catch { }
        }

        class PrefFile
        {
            public string Lang { get; set; }
            public string LastGame { get; set; }
            public Dictionary<string, string> Overrides { get; set; }
        }
    }
}
