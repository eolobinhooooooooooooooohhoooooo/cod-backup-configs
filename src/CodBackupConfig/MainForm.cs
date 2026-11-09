using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CodBackupConfig
{
    public class MainForm : Form
    {
        const int M = 24;
        const int W = 920;
        const int GAP = 12;

        const int COL1 = 24;
        const int COL2 = 286;
        const int COL3 = 618;

        ToolTip _tt;
        System.Windows.Forms.Timer _blink;
        bool _dotOn = true;
        Panel _dot;
        Label _title, _secGame, _secAccounts, _secBackups;
        Label _legendNote, _footer, _status;
        FlagButton _btnEn, _btnPt;
        GameCard[] _cards;
        LineButton _btnRefresh, _btnBackup, _btnRestore, _btnLock, _btnPlayers, _btnBackups;
        CheckBox _chkReplace;
        Label _lblName, _lblSnap, _lblTarget;
        TextBox _txtName, _txtTarget;
        ComboBox _cbSnap;
        BorderPanel _listBorder;
        ListBox _lb;

        List<GameInfo> _games;
        int _selGame = -1;
        List<Account> _accounts = new();
        List<Snapshot> _snaps = new();

        static string T(string key) => L10n.T(key);

        public MainForm()
        {
            Text = "Cod Backup Configs";
            BackColor = Ui.Bg;
            Font = Ui.Body;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(W, 624);
            DoubleBuffered = true;

            _tt = new ToolTip { AutoPopDelay = 10000, InitialDelay = 250, ShowAlways = true };
            _games = CodPaths.Games;

            BuildTopBar();
            BuildGameSection();
            BuildAccountsSection();
            BuildBackupsSection();
            BuildFooter();

            _blink = new System.Windows.Forms.Timer { Interval = 1400 };
            _blink.Tick += (s, e) => { _dotOn = !_dotOn; _dot.Invalidate(); };
            _blink.Start();

            Load += (s, e) =>
            {
                Prefs.Load();
                L10n.Lang = Prefs.Lang;
                ApplyTexts();
                int i = _games.FindIndex(g => g.Key == Prefs.LastGame);
                SelectGame(i < 0 ? 0 : i);
            };
            FormClosed += (s, e) => Prefs.Save();
        }

        // -------------------------------------------------------------- layout

        void BuildTopBar()
        {
            _dot = new Panel { Location = new Point(M, 17), Size = new Size(6, 6) };
            _dot.Paint += (s, e) =>
            {
                using var b = new SolidBrush(_dotOn ? Ui.Accent : Ui.Faint);
                e.Graphics.FillEllipse(b, 0, 0, 5, 5);
            };
            Controls.Add(_dot);

            _title = new Label
            {
                AutoSize = true,
                Font = Ui.Title,
                ForeColor = Ui.Text,
                BackColor = Color.Transparent,
                Location = new Point(M + 18, 10),
            };
            Controls.Add(_title);

            _btnPt = new FlagButton
            {
                Width = 64, Height = 30,
                Location = new Point(W - M - 64, 8),
                Font = Ui.Small,
                Flag = Assets.Img("flag-pt.png"),
            };
            _btnPt.Click += (s, e) => SetLang("pt");

            _btnEn = new FlagButton
            {
                Width = 64, Height = 30,
                Location = new Point(W - M - 136, 8),
                Font = Ui.Small,
                Flag = Assets.Img("flag-en.png"),
            };
            _btnEn.Click += (s, e) => SetLang("en");

            Controls.Add(_btnEn);
            Controls.Add(_btnPt);
        }

        void BuildGameSection()
        {
            _secGame = new SectionLabel { Location = new Point(M, 54) };
            Controls.Add(_secGame);

            int cw = (W - 2 * M - 2 * GAP) / 3;
            _cards = new GameCard[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _cards[i] = new GameCard(_games[i])
                {
                    Location = new Point(M + i * (cw + GAP), 74),
                    Size = new Size(cw, 116),
                };
                _cards[i].Clicked += () => SelectGame(idx);
                _cards[i].BrowseClick += BrowseFor;
                Controls.Add(_cards[i]);
            }

            // platform legend (logos ship inside the exe)
            int y = 200;
            AddPlatformLogo("battlenet.png", M, y, "battle.net", M + 20, y + 1);
            AddPlatformLogo("steam.png", M + 118, y, "steam", M + 138, y + 1);
            AddPlatformLogo("xbox.png", M + 216, y, "xbox", M + 236, y + 1);

            _legendNote = new Label
            {
                Font = Ui.Small,
                ForeColor = Ui.Faint,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(W - M - 380, y + 1),
                Size = new Size(380, 14),
            };
            Controls.Add(_legendNote);
        }

        void AddPlatformLogo(string asset, int lx, int ly, string name, int nx, int ny)
        {
            var pb = new PictureBox
            {
                Location = new Point(lx, ly),
                Size = new Size(14, 14),
                BackColor = Color.Transparent,
            };
            var src = Assets.Img(asset);
            if (src != null) pb.Image = Img.Scale(src, 14, 14);
            Controls.Add(pb);

            Controls.Add(new Label
            {
                AutoSize = true,
                Text = name,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Color.Transparent,
                Location = new Point(nx, ny),
            });
        }

        void BuildAccountsSection()
        {
            _secAccounts = new SectionLabel { Location = new Point(M, 230) };
            Controls.Add(_secAccounts);

            _btnRefresh = new LineButton
            {
                Font = Ui.Small,
                Location = new Point(W - M - 88, 224),
                Size = new Size(88, 26),
            };
            _btnRefresh.Click += (s, e) => { Rescan(); Status(T("st.ready")); };
            Controls.Add(_btnRefresh);

            _listBorder = new BorderPanel
            {
                Location = new Point(M, 250),
                Size = new Size(W - 2 * M, 148),
                BackColor = Ui.Panel,
            };
            _lb = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Ui.Panel,
                ForeColor = Ui.Text,
                Font = Ui.Body,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
            };
            _lb.DrawItem += Lb_DrawItem;
            _lb.SelectedIndexChanged += (s, e) =>
            {
                var a = SelAcct();
                if (a != null && !_txtTarget.Focused) _txtTarget.Text = a.Id;
                UpdateLockButton();
            };
            _listBorder.Controls.Add(_lb);
            Controls.Add(_listBorder);
        }

        Label FieldLabel(Point p)
        {
            var l = new Label
            {
                AutoSize = true,
                Font = Ui.Label,
                ForeColor = Ui.Faint,
                BackColor = Color.Transparent,
                Location = p,
            };
            Controls.Add(l);
            return l;
        }

        LineButton MakeButton(Point p, Size s)
        {
            var b = new LineButton { Font = Ui.Body, Location = p, Size = s };
            Controls.Add(b);
            return b;
        }

        void BuildBackupsSection()
        {
            _secBackups = new SectionLabel { Location = new Point(M, 412) };
            Controls.Add(_secBackups);

            _lblName = FieldLabel(new Point(COL1, 432));
            _lblSnap = FieldLabel(new Point(COL2, 432));
            _lblTarget = FieldLabel(new Point(COL3, 432));

            _txtName = new TextBox
            {
                Location = new Point(COL1, 448), Size = new Size(250, 28),
                BackColor = Ui.Panel, ForeColor = Ui.Text, Font = Ui.Body,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _cbSnap = new ComboBox
            {
                Location = new Point(COL2, 448), Size = new Size(320, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Ui.Panel, ForeColor = Ui.Text, Font = Ui.Body,
            };
            _txtTarget = new TextBox
            {
                Location = new Point(COL3, 448), Size = new Size(278, 28),
                BackColor = Ui.Panel, ForeColor = Ui.Text, Font = Ui.Body,
                BorderStyle = BorderStyle.FixedSingle,
            };
            Controls.Add(_txtName);
            Controls.Add(_cbSnap);
            Controls.Add(_txtTarget);

            _btnBackup = MakeButton(new Point(COL1, 486), new Size(190, 34));
            _btnBackup.Click += (s, e) => DoBackup();

            _btnRestore = MakeButton(new Point(COL1 + 190 + GAP, 486), new Size(210, 34));
            _btnRestore.Click += (s, e) => DoRestore();

            _chkReplace = new CheckBox
            {
                AutoSize = true,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Color.Transparent,
                Location = new Point(COL1 + 190 + GAP + 210 + 16, 494),
            };
            Controls.Add(_chkReplace);

            _btnLock = MakeButton(new Point(COL1, 526), new Size(170, 34));
            _btnLock.Click += (s, e) => DoLock();

            _btnPlayers = MakeButton(new Point(COL1 + 170 + GAP, 526), new Size(210, 34));
            _btnPlayers.Click += (s, e) => OpenFolder(SelGame()?.Resolved);

            _btnBackups = MakeButton(new Point(COL1 + 170 + GAP + 210 + GAP, 526), new Size(200, 34));
            _btnBackups.Click += (s, e) => OpenFolder(BackupStore.BackupsRoot);
        }

        void BuildFooter()
        {
            Controls.Add(new Panel
            {
                Location = new Point(M, 572),
                Size = new Size(W - 2 * M, 1),
                BackColor = Ui.Line,
            });
            _status = new Label
            {
                AutoSize = true,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Color.Transparent,
                Location = new Point(M, 584),
            };
            Controls.Add(_status);

            _footer = new Label
            {
                Font = Ui.Small,
                ForeColor = Ui.Faint,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 602),
                Size = new Size(W, 14),
            };
            Controls.Add(_footer);
        }

        // -------------------------------------------------------------- state

        GameInfo SelGame() =>
            _selGame >= 0 && _selGame < _games.Count ? _games[_selGame] : null;

        Account SelAcct()
        {
            int i = _lb.SelectedIndex;
            return i >= 0 && i < _accounts.Count ? _accounts[i] : null;
        }

        void SelectGame(int idx)
        {
            _selGame = idx;
            for (int i = 0; i < _cards.Length; i++) _cards[i].SetSelected(i == idx);
            Prefs.LastGame = SelGame().Key;
            Prefs.Save();
            Rescan();
        }

        void Rescan()
        {
            foreach (var g in _games)
            {
                string over = null;
                Prefs.FolderOverride.TryGetValue(g.Key, out over);
                g.Resolved = CodPaths.ResolveRoot(g, over);
            }

            _accounts.Clear();
            var sel = SelGame();
            if (sel != null && sel.Resolved != null)
                _accounts = CodPaths.ListAccounts(sel.Resolved);

            for (int i = 0; i < _cards.Length; i++)
            {
                var g = _games[i];
                int n = 0;
                if (g.Resolved != null)
                    n = ReferenceEquals(g, sel) ? _accounts.Count : CodPaths.ListAccounts(g.Resolved).Count;
                _cards[i].SetState(g.Resolved != null, n);
            }

            FillList();
            PopulateSnapshots();
        }

        void FillList()
        {
            string keep = SelAcct()?.Id;
            _lb.BeginUpdate();
            _lb.Items.Clear();
            if (_accounts.Count == 0)
                _lb.Items.Add(T("list.empty"));
            else
                foreach (var item in _accounts)
                    _lb.Items.Add(item.Id);
            _lb.EndUpdate();

            if (_accounts.Count > 0)
            {
                int idx = _accounts.FindIndex(x => x.Id == keep);
                _lb.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else
            {
                _lb.SelectedIndex = -1;
            }

            var a = SelAcct();
            if (a != null) _txtTarget.Text = a.Id;
            UpdateLockButton();
        }

        void UpdateLockButton()
        {
            var a = SelAcct();
            bool locked = a != null && a.LockedCount > a.FileCount / 2;
            _btnLock.Text = T(locked ? "btn.lockoff" : "btn.lockon");
        }

        void PopulateSnapshots()
        {
            var g = SelGame();
            _snaps = g != null ? BackupStore.ListSnapshots(g.Key) : new List<Snapshot>();

            string prev = _cbSnap.SelectedItem as string;
            _cbSnap.BeginUpdate();
            _cbSnap.Items.Clear();
            foreach (var s in _snaps)
                _cbSnap.Items.Add(SnapText(s));
            if (_snaps.Count == 0)
                _cbSnap.Items.Add(T("ph.none"));

            int idx = 0;
            if (!string.IsNullOrEmpty(prev))
            {
                for (int i = 0; i < _cbSnap.Items.Count; i++)
                    if (_cbSnap.Items[i].ToString() == prev) { idx = i; break; }
            }
            _cbSnap.SelectedIndex = idx;
            _cbSnap.EndUpdate();
        }

        static string SnapText(Snapshot s) =>
            $"{(string.IsNullOrEmpty(s.Label) ? "—" : s.Label)} · {s.AccountId} · {s.Created}";

        // -------------------------------------------------------------- actions

        void DoBackup()
        {
            var g = SelGame();
            if (g == null || g.Resolved == null) { Status(T("st.nofolders")); return; }
            var a = SelAcct();
            if (a == null) { Status(T("st.noacct")); return; }

            string label = _txtName.Text.Trim();
            if (label.Length > 40 ||
                label.Any(c => !(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')))
            {
                Status(T("st.badname"));
                return;
            }

            try
            {
                string dir = BackupStore.CreateSnapshot(g.Key, a.Dir, a.Id, label);
                StatusOk(string.Format(T("st.backupok"), dir));
                PopulateSnapshots();
            }
            catch (Exception ex) { StatusErr(ex.Message); }
        }

        void DoRestore()
        {
            var g = SelGame();
            if (g == null || g.Resolved == null) { Status(T("st.nofolders")); return; }

            int i = _cbSnap.SelectedIndex;
            if (i < 0 || i >= _snaps.Count) { Status(T("st.nosnap")); return; }

            string id = _txtTarget.Text.Trim();
            if (id.Length < 6 || id.Any(c => c < '0' || c > '9'))
            {
                Status(T("st.notarget"));
                return;
            }

            try
            {
                string target = Path.Combine(g.Resolved, id);
                bool made = !Directory.Exists(target);
                var r = BackupStore.Restore(_snaps[i].Dir, target, _chkReplace.Checked);
                string msg = string.Format(T("st.restoreok"), r.copied, r.skipped);
                if (made) msg += " · " + T("st.made");
                StatusOk(msg);
                Rescan();
            }
            catch (Exception ex) { StatusErr(ex.Message); }
        }

        void DoLock()
        {
            var a = SelAcct();
            if (a == null) { Status(T("st.noacct")); return; }
            try
            {
                var res = BackupStore.ToggleSettingsLock(a.Dir);
                if (res.count == 0) { Status(T("st.nosettings")); return; }
                StatusOk(string.Format(T(res.lockedNow ? "st.lockon" : "st.lockoff"), res.count));
                Rescan();
            }
            catch (Exception ex) { StatusErr(ex.Message); }
        }

        void OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Status(T("st.nofolders"));
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"")
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex) { StatusErr(ex.Message); }
        }

        void BrowseFor(GameInfo g)
        {
            using var d = new FolderBrowserDialog
            {
                Description = T("card.browse"),
                UseDescriptionForTitle = true,
            };
            if (d.ShowDialog(this) != DialogResult.OK) return;
            Prefs.FolderOverride[g.Key] = d.SelectedPath;
            Prefs.Save();
            Rescan();
        }

        void SetLang(string lang)
        {
            if (L10n.Lang == lang) return;
            L10n.Lang = lang;
            Prefs.Lang = lang;
            Prefs.Save();
            ApplyTexts();
        }

        void Status(string s)
        {
            _status.ForeColor = Ui.Dim;
            _status.Text = s;
        }

        void StatusOk(string s) => Status(s);

        void StatusErr(string s)
        {
            _status.ForeColor = Ui.Error;
            _status.Text = T("err") + s;
        }

        // -------------------------------------------------------------- texts

        void ApplyTexts()
        {
            Text = "Cod Backup Configs";
            _title.Text = T("title");
            _secGame.Text = T("sec.game");
            _secAccounts.Text = T("sec.accounts");
            _secBackups.Text = T("sec.backups");

            _btnEn.Highlight = L10n.Lang == "en";
            _btnPt.Highlight = L10n.Lang == "pt";

            _btnRefresh.Text = T("btn.refresh");
            _btnBackup.Text = T("btn.backup");
            _btnRestore.Text = T("btn.restore");
            _btnPlayers.Text = T("btn.players");
            _btnBackups.Text = T("btn.backups");
            UpdateLockButton();

            _lblName.Text = T("lbl.name");
            _lblSnap.Text = T("lbl.snapshot");
            _lblTarget.Text = T("lbl.target");
            _txtName.PlaceholderText = T("ph.name");
            _txtTarget.PlaceholderText = T("ph.target");
            _chkReplace.Text = T("chk.replace");
            _legendNote.Text = T("legend.note");

            FillList();
            PopulateSnapshots();

            _tt.SetToolTip(_btnEn, T("tip.en"));
            _tt.SetToolTip(_btnPt, T("tip.pt"));
            _tt.SetToolTip(_btnRefresh, T("tip.refresh"));
            _tt.SetToolTip(_btnBackup, T("tip.backup"));
            _tt.SetToolTip(_btnRestore, T("tip.restore"));
            _tt.SetToolTip(_btnLock, T("tip.lock"));
            _tt.SetToolTip(_btnPlayers, T("tip.players"));
            _tt.SetToolTip(_btnBackups, T("tip.backups"));
            _tt.SetToolTip(_txtName, T("tip.name"));
            _tt.SetToolTip(_cbSnap, T("tip.snapshot"));
            _tt.SetToolTip(_txtTarget, T("tip.target"));
            _tt.SetToolTip(_chkReplace, T("tip.replace"));
            _tt.SetToolTip(_lb, T("tip.list"));
            for (int i = 0; i < _cards.Length; i++)
            {
                _tt.SetToolTip(_cards[i], T("tip.game"));
                _tt.SetToolTip(_cards[i].BrowseButton, T("tip.browse"));
            }

            _lb.Invalidate();
            foreach (var c in _cards) c.Invalidate();

            Status(T("st.ready"));
            _footer.Text = T("footer");
        }

        // -------------------------------------------------------------- list draw

        void Lb_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            bool sel = e.Index == _lb.SelectedIndex;
            using (var back = new SolidBrush(sel ? Ui.AccentDim : Ui.Panel))
                g.FillRectangle(back, e.Bounds);

            if (e.Index >= _accounts.Count) // placeholder row
            {
                TextRenderer.DrawText(g, _lb.Items[e.Index].ToString(), Ui.Small,
                    new Point(e.Bounds.X + 10, e.Bounds.Y + 4), Ui.Faint,
                    TextFormatFlags.NoPadding);
                return;
            }

            var a = _accounts[e.Index];
            int x = e.Bounds.X + 10, y = e.Bounds.Y + 4;
            Seg(g, a.Id, Ui.Text, y, ref x);
            x += 10;
            Seg(g, a.Platform,
                a.Platform == "unknown" || a.Platform.EndsWith("?") ? Ui.Faint : Ui.Accent, y, ref x);
            x += 10;
            Seg(g, $"{a.FileCount} files", sel ? Ui.Text : Ui.Dim, y, ref x);
            if (a.LockedCount > 0)
            {
                x += 10;
                Seg(g, $"{a.LockedCount} locked", sel ? Ui.Text : Ui.Faint, y, ref x);
            }
        }

        void Seg(Graphics g, string s, Color c, int y, ref int x)
        {
            int w = TextRenderer.MeasureText(g, s, Ui.Body).Width;
            TextRenderer.DrawText(g, s, Ui.Body, new Point(x, y), c, TextFormatFlags.NoPadding);
            x += w;
        }
    }
}
