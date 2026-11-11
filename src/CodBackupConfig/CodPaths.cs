using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodBackupConfig
{
    public class GameInfo
    {
        public string Key;      // bo6 | bo7 | mw4
        public string Name;
        public string[] Roots;  // candidate players folders, in priority order
        public string Resolved; // picked root, null when nothing was found
    }

    public class Account
    {
        public string Id;
        public string Dir;
        public int FileCount;
        public int SettingsCount; // settings files (everything except .swp swap files)
        public int LockedCount;   // read-only among the settings files only
        public string Platform;
    }

    public static class CodPaths
    {
        // BO7+ share one tree under LocalAppData
        static string NewGenRoot =>
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Activision\Call of Duty\players");

        // the old COD-HQ tree that BO6 wrote to
        static string OldGenRoot =>
            Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\Call of Duty\players");

        public static List<GameInfo> Games { get; } = new()
        {
            new GameInfo
            {
                Key = "bo6",
                Name = "Black Ops 6",
                Roots = new[]
                {
                    OldGenRoot,
                    NewGenRoot,
                    Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Activision\blackops6"),
                    Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Activision\black ops 6"),
                },
            },
            new GameInfo
            {
                Key = "bo7",
                Name = "Black Ops 7",
                Roots = new[]
                {
                    NewGenRoot,
                    Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Activision\blackops7"),
                },
            },
            new GameInfo
            {
                Key = "mw4",
                Name = "Modern Warfare 4",
                Roots = new[]
                {
                    NewGenRoot,
                    Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Activision\modernwarfare4"),
                    Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Activision\call of duty modern warfare 4"),
                },
            },
        };

        // first candidate that exists AND holds at least one account folder
        public static string ResolveRoot(GameInfo game, string manualOverride = null)
        {
            if (!string.IsNullOrEmpty(manualOverride) && Directory.Exists(manualOverride))
                return manualOverride;

            foreach (string root in game.Roots)
                if (Directory.Exists(root) && HasAccounts(root))
                    return root;

            return null;
        }

        static bool HasAccounts(string root)
        {
            try
            {
                return Directory.GetDirectories(root).Any(IsAccountDir);
            }
            catch
            {
                return false;
            }
        }

        // an account is a subfolder named with digits only
        public static bool IsAccountDir(string path)
        {
            string name = Path.GetFileName(path);
            return name.Length >= 6 && name.All(c => c >= '0' && c <= '9');
        }

        // every file in an account folder is a setting except .swp swap
        // files: the game rewrites all of them on launch/update, and the
        // read-only lock exists to stop exactly that. naming differs per
        // generation (s.1.0.*, g.cod25.*, g.cod26.*, keybinds.*, ...), so
        // the lock must not depend on any one prefix
        public static bool IsSettingsFile(string fileName) =>
            !fileName.EndsWith(".swp", StringComparison.OrdinalIgnoreCase);

        public static List<Account> ListAccounts(string root)
        {
            var list = new List<Account>();
            try
            {
                foreach (string dir in Directory.GetDirectories(root))
                {
                    if (!IsAccountDir(dir)) continue;
                    string id = Path.GetFileName(dir);
                    int files = 0, settings = 0, locked = 0;
                    try
                    {
                        foreach (string f in Directory.GetFiles(dir))
                        {
                            files++;
                            // lock state is tracked on the settings files
                            // only; .swp swap files mean nothing here
                            if (IsSettingsFile(Path.GetFileName(f)))
                            {
                                settings++;
                                if ((File.GetAttributes(f) & FileAttributes.ReadOnly) != 0) locked++;
                            }
                        }
                    }
                    catch { }
                    list.Add(new Account
                    {
                        Id = id,
                        Dir = dir,
                        FileCount = files,
                        SettingsCount = settings,
                        LockedCount = locked,
                        Platform = GuessPlatform(id),
                    });
                }
            }
            catch { }
            return list.OrderBy(a => a.Id).ToList();
        }

        // shape-based guess. Confidence notes in docs/RESEARCH.md.
        // This is a label only — detection itself never relies on it.
        public static string GuessPlatform(string id)
        {
            int n = id.Length;
            if (n >= 17 && n <= 19 && id.StartsWith("7656")) return "steam";
            if (n >= 9 && n <= 11) return "battle.net";
            if (n >= 12) return "xbox?";
            return "unknown";
        }
    }
}
