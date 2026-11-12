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
        public bool Available;  // false = card shown grayed out, unclickable
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
                Available = false,
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
                Available = false,
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
                Available = true,
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

        // shader/xpak caches: the game regenerates them on launch, so they
        // are never backed up and never shown in counts
        public static bool IsCacheDir(string dirName)
        {
            string n = dirName.ToLowerInvariant();
            return n == "shadercache" || n == "xpak_cache";
        }

        // campaign save folder (sp1, sp12, ...): backed up with the
        // account, but never read-only-locked — locking saves has been
        // reported as a cause of lost campaign progress
        public static bool IsSaveDir(string dirName)
        {
            if (dirName.Length < 3 || !dirName.StartsWith("sp", StringComparison.OrdinalIgnoreCase))
                return false;
            for (int i = 2; i < dirName.Length; i++)
                if (dirName[i] < '0' || dirName[i] > '9') return false;
            return true;
        }

        // every file under an account folder, top level included, with the
        // relative layout intact. cache folders are always skipped; save
        // folders (sp*) only when includeSaves is false. one folder failing
        // mid-scan (the game may hold it open) is dropped, not fatal
        public static List<string> EnumerateFiles(string dir, bool includeSaves)
        {
            var files = new List<string>();
            if (!Directory.Exists(dir)) return files;
            var stack = new Stack<string>();
            stack.Push(dir);
            while (stack.Count > 0)
            {
                string cur = stack.Pop();
                try
                {
                    foreach (string f in Directory.GetFiles(cur)) files.Add(f);
                    foreach (string d in Directory.GetDirectories(cur))
                    {
                        string n = Path.GetFileName(d);
                        if (IsCacheDir(n)) continue;
                        if (!includeSaves && IsSaveDir(n)) continue;
                        stack.Push(d);
                    }
                }
                catch { }
            }
            return files;
        }

        // settings files the game keeps loose in the players root itself
        // (mw4 keeps s.1.0.cod26.txt there): shared between accounts, so
        // counts, snapshots and the lock all include them
        public static List<string> ListRootSettingsFiles(string root)
        {
            var files = new List<string>();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return files;
            try
            {
                foreach (string f in Directory.GetFiles(root))
                    if (IsSettingsFile(Path.GetFileName(f))) files.Add(f);
            }
            catch { }
            return files;
        }

        public static List<Account> ListAccounts(string root)
        {
            var list = new List<Account>();
            List<string> rootFiles = null; // lazy: computed once, shared by every row
            int rootLocked = 0;
            try
            {
                foreach (string dir in Directory.GetDirectories(root))
                {
                    if (!IsAccountDir(dir)) continue;
                    string id = Path.GetFileName(dir);
                    List<string> files = EnumerateFiles(dir, includeSaves: true);
                    int settings = 0, locked = 0;
                    foreach (string f in files)
                    {
                        // lock state is tracked on the settings files only;
                        // .swp swap files mean nothing here
                        if (!IsSettingsFile(Path.GetFileName(f))) continue;
                        settings++;
                        // save folders (sp*) never participate in the lock,
                        // so their read-only state doesn't count as locked
                        if (IsSaveDir(Path.GetFileName(Path.GetDirectoryName(f)))) continue;
                        if ((File.GetAttributes(f) & FileAttributes.ReadOnly) != 0) locked++;
                    }
                    if (rootFiles == null)
                    {
                        rootFiles = ListRootSettingsFiles(root);
                        foreach (string f in rootFiles)
                            if ((File.GetAttributes(f) & FileAttributes.ReadOnly) != 0) rootLocked++;
                    }
                    list.Add(new Account
                    {
                        Id = id,
                        Dir = dir,
                        FileCount = files.Count + rootFiles.Count,
                        SettingsCount = settings + rootFiles.Count,
                        LockedCount = locked + rootLocked,
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
