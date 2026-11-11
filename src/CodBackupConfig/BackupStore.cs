using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CodBackupConfig
{
    public class Snapshot
    {
        public string Dir;
        public string Label;
        public string AccountId;
        public string Created; // yyyy-MM-dd HH:mm
    }

    // copy-only store. no moves, no deletes, ever.
    public static class BackupStore
    {
        // getfolderpath reads the registry, not the %localappdata% env var;
        // honor the env var like codpaths does so both roots stay consistent.
        public static string Root => Path.Combine(LocalAppData(), "CodBackupConfigs");

        static string LocalAppData()
        {
            string v = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            return string.IsNullOrEmpty(v)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : v;
        }

        public static string BackupsRoot => Path.Combine(Root, "backups");

        public static string GameBackupsRoot(string game) => Path.Combine(BackupsRoot, game);

        // one file, attributes preserved (read-only stays read-only)
        static void CopyFile(string src, string destDir)
        {
            string dst = Path.Combine(destDir, Path.GetFileName(src));
            File.Copy(src, dst);
            new FileInfo(dst).IsReadOnly = new FileInfo(src).IsReadOnly;
        }

        public static string CreateSnapshot(string game, string accountDir, string accountId, string label)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string parent = Path.Combine(GameBackupsRoot(game), accountId);
            string dest = Path.Combine(parent, stamp);
            int n = 1;
            while (Directory.Exists(dest)) // same-second collision, just keep numbering
                dest = Path.Combine(parent, $"{stamp}-{n++}");
            Directory.CreateDirectory(dest);

            int files = 0;
            foreach (string f in Directory.GetFiles(accountDir))
            {
                CopyFile(f, dest);
                files++;
            }

            var meta = new Dictionary<string, string>
            {
                ["game"] = game,
                ["account"] = accountId,
                ["label"] = label ?? "",
                ["created"] = DateTime.Now.ToString("o"),
                ["files"] = files.ToString(),
            };
            File.WriteAllText(Path.Combine(dest, "meta.json"),
                JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
            return dest;
        }

        public static List<Snapshot> ListSnapshots(string game)
        {
            var list = new List<Snapshot>();
            string root = GameBackupsRoot(game);
            if (!Directory.Exists(root)) return list;

            foreach (string acc in Directory.GetDirectories(root))
            {
                string id = Path.GetFileName(acc);
                foreach (string snap in Directory.GetDirectories(acc))
                {
                    var s = new Snapshot { Dir = snap, AccountId = id };
                    try
                    {
                        string m = Path.Combine(snap, "meta.json");
                        if (File.Exists(m))
                        {
                            var meta = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(m));
                            if (meta != null)
                            {
                                if (meta.TryGetValue("label", out var l) && l.ValueKind == JsonValueKind.String)
                                    s.Label = l.GetString();
                                if (meta.TryGetValue("created", out var c) && c.ValueKind == JsonValueKind.String)
                                    s.Created = SafeDate(c.GetString());
                            }
                        }
                    }
                    catch { }
                    if (string.IsNullOrEmpty(s.Created))
                        s.Created = Directory.GetCreationTime(snap).ToString("yyyy-MM-dd HH:mm");
                    list.Add(s);
                }
            }
            return list.OrderByDescending(s => s.Created).ThenByDescending(s => s.Dir).ToList();
        }

        static string SafeDate(string iso) =>
            DateTime.TryParse(iso, out var d) ? d.ToString("yyyy-MM-dd HH:mm") : iso;

        public static (int copied, int skipped) Restore(string snapshotDir, string targetDir, bool overwrite)
        {
            Directory.CreateDirectory(targetDir);
            int copied = 0, skipped = 0;

            foreach (string src in Directory.GetFiles(snapshotDir))
            {
                if (string.Equals(Path.GetFileName(src), "meta.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                string dst = Path.Combine(targetDir, Path.GetFileName(src));
                if (File.Exists(dst) && !overwrite) { skipped++; continue; }
                if (File.Exists(dst)) File.SetAttributes(dst, FileAttributes.Normal);
                File.Copy(src, dst, true);
                new FileInfo(dst).IsReadOnly = new FileInfo(src).IsReadOnly;
                copied++;
            }
            return (copied, skipped);
        }

        // flip read-only on the settings files of one account (everything
        // except .swp swap files — see CodPaths.IsSettingsFile)
        public static (bool lockedNow, int count) ToggleSettingsLock(string accountDir)
        {
            var files = Directory.GetFiles(accountDir)
                .Where(f => CodPaths.IsSettingsFile(Path.GetFileName(f)))
                .ToList();
            if (files.Count == 0) return (false, 0);

            int locked = files.Count(f => (File.GetAttributes(f) & FileAttributes.ReadOnly) != 0);
            bool lockThem = locked <= files.Count / 2; // majority free -> lock

            foreach (string f in files)
            {
                var attr = File.GetAttributes(f) & ~FileAttributes.ReadOnly;
                if (lockThem) attr |= FileAttributes.ReadOnly;
                File.SetAttributes(f, attr);
            }
            return (lockThem, files.Count);
        }
    }
}
