using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodBackupConfig
{
    public class MainForm : Form
    {
        const int M = 24;
        const int W = 940;
        const int GAP = 12;

        const int PANEL_W = (W - 2 * M - GAP) / 2;
        const int PANEL_H = 146;
        const int PAD = 14;

        ToolTip _tt;
        System.Windows.Forms.Timer _blink;
        bool _dotOn = true;
        Panel _dot;
        Label _title, _ver, _secGame, _secAccounts, _secBackups;
        Label _status;
        FlagButton _btnEn, _btnPt;
        GameCard[] _cards;
        LineButton _btnRefresh, _btnBackup, _btnRestore, _btnLock, _btnPlayers, _btnBackups;
        TickBox _chkReplace;
        TextBox _txtName, _txtTarget;
        ChoiceBox _cbSnap;
        BorderPanel _listBorder, _savePanel, _restorePanel;
        Label _saveTag, _saveHint, _saveNote, _restoreTag, _restoreHint;
        Panel _footerLine;
        ListBox _lb;
        int _lbPrevSel = -1;

        List<GameInfo> _games;
        int _selGame = -1;
        List<Account> _accounts = new();
        List<Snapshot> _snaps = new();

        static string T(string key) => L10n.T(key);

        // platform marks per guessed platform, cached after first load
        static readonly Dictionary<string, Bitmap> _marks = new(StringComparer.OrdinalIgnoreCase);

        // button widths fit the longest en+pt label measured in hitmarker 9pt bold
        const int BTN_REFRESH = 112;
        const int BTN_BACKUP = 200;
        const int BTN_LOCK = 204;
        const int BTN_PLAYERS = 210;
        const int BTN_BACKUPS = 200;
        const int CHK_W = 210;

        // uniform scale factor for every hardcoded pixel in the ui; fonts stay
        // in points and scale with the device context on their own
        float DpiScale()
        {
            try
            {
                var _ = Handle; // the window dc must exist to report an honest dpi
                using Graphics g = CreateGraphics();
                return g.DpiX / 96f;
            }
            catch { return 1f; }
        }

        static Bitmap PlatformMark(string platform)
        {
            string key =
                platform == "battle.net"      ? "platform-battlenet.png" :
                platform.StartsWith("steam")   ? "platform-steam.png" :
                platform.StartsWith("xbox")    ? "platform-xbox.png" : null;
            if (key == null) return null;
            if (!_marks.TryGetValue(key, out var m))
                _marks[key] = m = Assets.Img(key);
            return m;
        }

        public MainForm()
        {
            // winforms' dpi autoscale fights the gpu-scaled device context, so
            // the framework stays out and geometry scales through Ui.X instead
            AutoScaleMode = AutoScaleMode.None;
            Ui.F = DpiScale();
            Text = "Cod Backup Configs";
            BackColor = Ui.Bg;
            Font = Ui.Body;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.X(W), Ui.X(576));
            DoubleBuffered = true;

            // window chrome icon from the kit art; the hicon lives exactly
            // as long as the process, so nothing needs releasing here
            var icoBmp = Assets.Img("icon-256.png");
            if (icoBmp != null)
            {
                Icon = System.Drawing.Icon.FromHandle(icoBmp.GetHicon());
                icoBmp.Dispose();
            }

            _tt = new ToolTip { AutoPopDelay = 10000, InitialDelay = 250, ShowAlways = true };
            _games = CodPaths.Games;

            BuildTopBar();
            BuildGameSection();
            BuildAccountsSection();
            BuildBackupsSection();
            BuildFooter();
            ApplyLayout();

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

        // film grain background; backcolor stays flat for resize-flash protection
        protected override void OnPaintBackground(PaintEventArgs e) =>
            e.Graphics.FillRectangle(GrainFx.Brush, ClientRectangle);

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // dark window caption so the chrome matches the theme
            try
            {
                int one = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref one, 4) != 0) // Win10 20H1+ / Win11
                    DwmSetWindowAttribute(Handle, 19, ref one, 4);      // older builds
            }
            catch { }
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            // uniform rescale of every control, then rebuild the dynamic part
            Ui.F = DpiScale();
            ApplyLayout();
            LayoutBottom();
        }

        // -------------------------------------------------------------- layout

        void BuildTopBar()
        {
            _dot = new Panel { Location = new Point(M, 22), Size = new Size(6, 6) };
            _dot.Paint += (s, e) =>
            {
                using var b = new SolidBrush(_dotOn ? Ui.Accent : Ui.Faint);
                e.Graphics.FillEllipse(b, 0, 0, Ui.X(5), Ui.X(5));
            };
            Controls.Add(_dot);

            _title = new Label
            {
                AutoSize = true,
                Font = Ui.Title,
                ForeColor = Ui.Text,
                BackColor = Color.Transparent,
                Location = new Point(M + 18, 14),
            };
            Controls.Add(_title);

            _ver = new Label
            {
                AutoSize = true,
                Text = "v" + typeof(MainForm).Assembly.GetName().Version.ToString(3),
                Font = Ui.Small,
                ForeColor = Ui.Faint,
                BackColor = Color.Transparent,
                Location = new Point(M + 200, 19),
            };
            Controls.Add(_ver);

            _btnPt = new FlagButton
            {
                Width = 60, Height = 28,
                Location = new Point(W - M - 60, 10),
                Font = Ui.Small,
                Flag = Assets.Img("flag-pt.png"),
            };
            _btnPt.Click += (s, e) => SetLang("pt");

            _btnEn = new FlagButton
            {
                Width = 60, Height = 28,
                Location = new Point(W - M - 128, 10),
                Font = Ui.Small,
                Flag = Assets.Img("flag-en.png"),
            };
            _btnEn.Click += (s, e) => SetLang("en");

            Controls.Add(_btnEn);
            Controls.Add(_btnPt);
        }

        void BuildGameSection()
        {
            _secGame = new SectionLabel { Location = new Point(M, 56) };
            Controls.Add(_secGame);

            int cw = (W - 2 * M - 2 * GAP) / 3;
            _cards = new GameCard[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _cards[i] = new GameCard(_games[i])
                {
                    Location = new Point(M + i * (cw + GAP), 74),
                    Size = new Size(cw, 108),
                };
                _cards[i].Clicked += () => SelectGame(idx);
                _cards[i].BrowseClick += BrowseFor;
                Controls.Add(_cards[i]);
            }
        }

        void BuildAccountsSection()
        {
            _secAccounts = new SectionLabel { Location = new Point(M, 196) };
            Controls.Add(_secAccounts);

            _btnRefresh = new LineButton
            {
                Font = Ui.Btn,
                Location = new Point(W - M - 88, 190),
                Size = new Size(88, 26),
            };
            _btnRefresh.Icon = Assets.Img("ic-refresh.png");
            _btnRefresh.Click += (s, e) => { Rescan(); Status(T("st.ready")); };
            Controls.Add(_btnRefresh);

            _listBorder = new BorderPanel
            {
                Location = new Point(M, 216),
                Size = new Size(W - 2 * M, 116),
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
                ItemHeight = 26,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
            };
            _lb.DrawItem += Lb_DrawItem;
            _lb.SelectedIndexChanged += (s, e) =>
            {
                int cur = _lb.SelectedIndex;
                int prev = _lbPrevSel;
                _lbPrevSel = cur;
                // the framework occasionally skips the deselect repaint of the
                // previous row, leaving a ghost highlight until full activation
                if (prev >= 0 && prev != cur) _lb.Invalidate();
                var a = SelAcct();
                if (a != null && !_txtTarget.Focused) _txtTarget.Text = a.Id;
                UpdateLockButton();
                UpdateSaveHint();
            };
            _lb.GotFocus += (s, e) => _lb.Invalidate();
            _lb.LostFocus += (s, e) => _lb.Invalidate();
            _listBorder.Controls.Add(_lb);
            Controls.Add(_listBorder);
        }

        LineButton MakeButton(Point p, Size s)
        {
            var b = new LineButton { Font = Ui.Btn, Location = p, Size = s };
            Controls.Add(b);
            return b;
        }

        void BuildBackupsSection()
        {
            _secBackups = new SectionLabel { Location = new Point(M, 342) };
            Controls.Add(_secBackups);

            // left box: saving a backup of the selected account
            _savePanel = ActionPanel(new Point(M, 360));
            _saveTag = PanelTag(_savePanel, "sec.save.tag");
            _saveHint = PanelHint(_savePanel);
            _txtName = new TextBox
            {
                Location = new Point(PAD, 50), Size = new Size(PANEL_W - 2 * PAD, 26),
                BackColor = Ui.Panel, ForeColor = Ui.Text, Font = Ui.Body,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _savePanel.Controls.Add(_txtName);
            _saveNote = new Label
            {
                AutoSize = true, Font = Ui.Small, ForeColor = Ui.Faint,
                BackColor = Color.Transparent, Location = new Point(PAD, 90),
            };
            _savePanel.Controls.Add(_saveNote);
            // block A: name + backup + lock live in this panel, side by side
            _btnBackup = PanelButton(_savePanel, DoBackup, new Point(PAD, 116), new Size(200, 28));
            _btnBackup.Icon = Assets.Img("ic-backup.png");
            _btnLock = PanelButton(_savePanel, DoLock, new Point(PAD + 208, 116), new Size(204, 28));
            _btnLock.Icon = Assets.Img("ic-lock.png");

            // right box: restoring an earlier snapshot
            _restorePanel = ActionPanel(new Point(M + PANEL_W + GAP, 360));
            _restoreTag = PanelTag(_restorePanel, "sec.restore.tag");
            _restoreHint = PanelHint(_restorePanel);
            _cbSnap = new ChoiceBox
            {
                Location = new Point(PAD, 50), Size = new Size(PANEL_W - 2 * PAD, 26),
                Font = Ui.Body,
            };
            _cbSnap.Changed += (i) =>
            {
                if (!_txtTarget.Focused && i >= 0 && i < _snaps.Count)
                    _txtTarget.Text = _snaps[i].AccountId;
            };
            _restorePanel.Controls.Add(_cbSnap);
            int tW = PANEL_W - 2 * PAD - 210 - 8;
            _txtTarget = new TextBox
            {
                Location = new Point(PAD, 84), Size = new Size(tW, 26),
                BackColor = Ui.Panel, ForeColor = Ui.Text, Font = Ui.Mono(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            _restorePanel.Controls.Add(_txtTarget);
            _chkReplace = new TickBox
            {
                Font = Ui.Small, BackColor = Ui.Panel,
                Location = new Point(PAD + tW + 8, 89), Size = new Size(210, 16),
            };
            _restorePanel.Controls.Add(_chkReplace);
            _btnRestore = PanelButton(_restorePanel, DoRestore, new Point(PAD, 116), new Size(PANEL_W - 2 * PAD, 28));
            _btnRestore.Icon = Assets.Img("ic-restore.png");

            _btnPlayers = MakeButton(new Point(M, 520), new Size(210, 28));
            _btnPlayers.Icon = Assets.Img("ic-folder.png");
            _btnPlayers.Click += (s, e) => OpenFolder(SelGame()?.Resolved);

            _btnBackups = MakeButton(new Point(M + 210 + GAP, 520), new Size(190, 28));
            _btnBackups.Icon = Assets.Img("ic-folder.png");
            _btnBackups.Click += (s, e) => OpenFolder(BackupStore.BackupsRoot);
        }

        BorderPanel ActionPanel(Point p)
        {
            var b = new BorderPanel
            {
                Location = p, Size = new Size(PANEL_W, PANEL_H),
                BackColor = Ui.Panel,
            };
            Controls.Add(b);
            return b;
        }

        Label PanelTag(BorderPanel p, string key)
        {
            var l = new Label
            {
                AutoSize = true, Font = Ui.Label, ForeColor = Ui.Faint,
                BackColor = Color.Transparent, Text = T(key),
                Location = new Point(PAD, 12),
            };
            p.Controls.Add(l);
            return l;
        }

        Label PanelHint(BorderPanel p)
        {
            var l = new Label
            {
                AutoSize = true, Font = Ui.Small, ForeColor = Ui.Dim,
                BackColor = Color.Transparent, Location = new Point(PAD, 30),
            };
            p.Controls.Add(l);
            return l;
        }

        LineButton PanelButton(BorderPanel p, Action act, Point pos, Size size)
        {
            var b = new LineButton
            {
                Font = Ui.Btn, Location = pos,
                Size = size,
            };
            b.Click += (s, e) => act();
            p.Controls.Add(b);
            return b;
        }

        void BuildFooter()
        {
            _footerLine = new Panel
            {
                Location = new Point(M, 596),
                Size = new Size(W - 2 * M, 1),
                BackColor = Ui.Line,
            };
            Controls.Add(_footerLine);
            _status = new Label
            {
                AutoSize = true,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Color.Transparent,
                Location = new Point(M, 608),
            };
            Controls.Add(_status);
        }

        // positions every control for the current Ui.F; runs once from the
        // constructor and again when the window lands on another dpi
        void ApplyLayout()
        {
            _dot.Location = new Point(Ui.X(M), Ui.X(22));
            _dot.Size = new Size(Ui.X(6), Ui.X(6));
            _title.Location = new Point(Ui.X(M + 18), Ui.X(14));
            _btnPt.Location = new Point(Ui.X(W - M - 60), Ui.X(10));
            _btnPt.Size = new Size(Ui.X(60), Ui.X(28));
            _btnEn.Location = new Point(Ui.X(W - M - 128), Ui.X(10));
            _btnEn.Size = new Size(Ui.X(60), Ui.X(28));

            _secGame.Location = new Point(Ui.X(M), Ui.X(56));
            int cw = (W - 2 * M - 2 * GAP) / 3;
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].Location = new Point(Ui.X(M + i * (cw + GAP)), Ui.X(74));
                _cards[i].Size = new Size(Ui.X(cw), Ui.X(108));
            }

            _secAccounts.Location = new Point(Ui.X(M), Ui.X(196));
            _btnRefresh.Location = new Point(Ui.X(W - M - BTN_REFRESH), Ui.X(190));
            _btnRefresh.Size = new Size(Ui.X(BTN_REFRESH), Ui.X(26));
            _listBorder.Location = new Point(Ui.X(M), Ui.X(216));
            _listBorder.Size = new Size(Ui.X(W - 2 * M), Ui.X(116));

            _secBackups.Location = new Point(Ui.X(M), Ui.X(342));
            _savePanel.Location = new Point(Ui.X(M), Ui.X(360));
            _savePanel.Size = new Size(Ui.X(PANEL_W), Ui.X(PANEL_H));
            _restorePanel.Location = new Point(Ui.X(M + PANEL_W + GAP), Ui.X(360));
            _restorePanel.Size = new Size(Ui.X(PANEL_W), Ui.X(PANEL_H));
            LayoutPanels();

            _btnPlayers.Location = new Point(Ui.X(M), Ui.X(520));
            _btnPlayers.Size = new Size(Ui.X(BTN_PLAYERS), Ui.X(28));
            _btnBackups.Location = new Point(Ui.X(M + BTN_PLAYERS + GAP), Ui.X(520));
            _btnBackups.Size = new Size(Ui.X(BTN_BACKUPS), Ui.X(28));

            _footerLine.Location = new Point(Ui.X(M), Ui.X(596));
            _footerLine.Size = new Size(Ui.X(W - 2 * M), 1);
            _status.Location = new Point(Ui.X(M), Ui.X(608));

            ClientSize = new Size(Ui.X(W), ClientSize.Height);
        }

        // children of the two action panels, in panel coordinates
        void LayoutPanels()
        {
            _saveTag.Location = new Point(Ui.X(PAD), Ui.X(12));
            _saveHint.Location = new Point(Ui.X(PAD), Ui.X(30));
            _txtName.Location = new Point(Ui.X(PAD), Ui.X(50));
            _txtName.Size = new Size(Ui.X(PANEL_W - 2 * PAD), Ui.X(26));
            _saveNote.Location = new Point(Ui.X(PAD), Ui.X(90));
            _btnBackup.Location = new Point(Ui.X(PAD), Ui.X(116));
            _btnBackup.Size = new Size(Ui.X(BTN_BACKUP), Ui.X(28));
            _btnLock.Location = new Point(Ui.X(PAD + BTN_BACKUP + 8), Ui.X(116));
            _btnLock.Size = new Size(Ui.X(BTN_LOCK), Ui.X(28));

            _restoreTag.Location = new Point(Ui.X(PAD), Ui.X(12));
            _restoreHint.Location = new Point(Ui.X(PAD), Ui.X(30));
            _cbSnap.Location = new Point(Ui.X(PAD), Ui.X(50));
            _cbSnap.Size = new Size(Ui.X(PANEL_W - 2 * PAD), Ui.X(26));
            int tW = PANEL_W - 2 * PAD - CHK_W - 8;
            _txtTarget.Location = new Point(Ui.X(PAD), Ui.X(84));
            _txtTarget.Size = new Size(Ui.X(tW), Ui.X(26));
            _chkReplace.Location = new Point(Ui.X(PAD + tW + 8), Ui.X(89));
            _chkReplace.Size = new Size(Ui.X(CHK_W), Ui.X(16));
            _btnRestore.Location = new Point(Ui.X(PAD), Ui.X(116));
            _btnRestore.Size = new Size(Ui.X(PANEL_W - 2 * PAD), Ui.X(28));
        }

        // stretch or shrink the window so the list only takes as much
        // space as it has accounts in it
        void LayoutBottom()
        {
            int rowH = Ui.X(26);
            int rows = Math.Min(Math.Max(_accounts.Count, 1), 4);
            _lb.ItemHeight = rowH;
            _listBorder.Height = rows * rowH + Ui.X(8);

            int secY = _listBorder.Bottom + Ui.X(14);
            _secBackups.Location = new Point(Ui.X(M), secY);

            int pY = secY + Ui.X(18);
            _savePanel.Location = new Point(Ui.X(M), pY);
            _restorePanel.Location = new Point(Ui.X(M + PANEL_W + GAP), pY);

            int uY = pY + _savePanel.Height + Ui.X(14);
            _btnPlayers.Location = new Point(Ui.X(M), uY);
            _btnBackups.Location = new Point(Ui.X(M + BTN_PLAYERS + GAP), uY);

            int lY = uY + Ui.X(28) + Ui.X(12);
            _footerLine.Location = new Point(Ui.X(M), lY);
            _footerLine.Width = Ui.X(W - 2 * M);
            _status.Location = new Point(Ui.X(M), lY + Ui.X(12));

            ClientSize = new Size(ClientSize.Width, lY + Ui.X(42));
        }

        void UpdateSaveHint()
        {
            var a = SelAcct();
            _saveHint.Text = a == null
                ? T("sec.save.noselect")
                : string.Format(T("sec.save.hint"), a.Id, a.Platform, a.FileCount);
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
            UpdateSaveHint();
            LayoutBottom();
            _lbPrevSel = _lb.SelectedIndex;
            _lb.Invalidate();
        }

        void UpdateLockButton()
        {
            var a = SelAcct();
            // LockedCount tracks the s.1.0.* settings family only, so any
            // locked setting already means "this account is locked"
            bool locked = a != null && a.LockedCount > 0;
            // icon matches the action the button will perform
            _btnLock.Text = T(locked ? "btn.lockoff" : "btn.lockon");
            _btnLock.Icon = Assets.Img(locked ? "ic-unlock.png" : "ic-lock.png");
        }

        void PopulateSnapshots()
        {
            var g = SelGame();
            string oldText = null;
            int oldSel = _cbSnap.SelectedIndex;
            if (oldSel >= 0 && oldSel < _snaps.Count)
                oldText = SnapText(_snaps[oldSel]);

            _snaps = g != null ? BackupStore.ListSnapshots(g.Key) : new List<Snapshot>();

            _cbSnap.Items.Clear();
            var icons = new Bitmap[_snaps.Count];
            for (int i = 0; i < _snaps.Count; i++)
            {
                _cbSnap.Items.Add(SnapText(_snaps[i]));
                icons[i] = PlatformMark(CodPaths.GuessPlatform(_snaps[i].AccountId));
            }
            _cbSnap.Icons = icons;

            int idx = _cbSnap.Items.Count > 0 ? 0 : -1;
            if (!string.IsNullOrEmpty(oldText))
            {
                idx = -1;
                for (int i = 0; i < _cbSnap.Items.Count; i++)
                    if (_cbSnap.Items[i] == oldText) { idx = i; break; }
            }
            _cbSnap.SelectedIndex = idx;
            _cbSnap.Invalidate();
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
            _ver.Location = new Point(_title.Right + Ui.X(10), Ui.X(19));
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

            _saveTag.Text = T("sec.save.tag");
            _restoreTag.Text = T("sec.restore.tag");
            _restoreHint.Text = T("sec.restore.hint");
            _saveNote.Text = T("sec.save.note");
            _txtName.PlaceholderText = T("ph.name");
            _txtTarget.PlaceholderText = T("ph.target");
            _chkReplace.Text = T("chk.replace");
            _cbSnap.Placeholder = T("ph.none");
            _cbSnap.Invalidate();

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
            _tt.SetToolTip(_lb, T("list.tip"));
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].BrowseButton.Text = T("card.browse");
                _tt.SetToolTip(_cards[i], T("tip.game"));
                _tt.SetToolTip(_cards[i].BrowseButton, T("tip.browse"));
            }

            _lb.Invalidate();
            foreach (var c in _cards) c.Invalidate();

            Status(T("st.ready"));
        }

        // -------------------------------------------------------------- list draw

        void Lb_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            bool sel = e.Index == _lb.SelectedIndex;
            using (var back = new SolidBrush(sel ? Ui.SelRow : Ui.Panel))
                g.FillRectangle(back, e.Bounds);
            if (sel)
                using (var b = new SolidBrush(Ui.Accent))
                    g.FillRectangle(b, e.Bounds.X, e.Bounds.Y, Ui.X(2), e.Bounds.Height);

            if (e.Index >= _accounts.Count) // placeholder row
            {
                TextRenderer.DrawText(g, _lb.Items[e.Index].ToString(), Ui.Small,
                    new Point(e.Bounds.X + Ui.X(12), e.Bounds.Y + Ui.X(6)), Ui.Faint,
                    TextFormatFlags.NoPadding);
                return;
            }

            var a = _accounts[e.Index];
            int bx = e.Bounds.X, by = e.Bounds.Y + Ui.X(6);
            int tx = bx + Ui.X(12);
            var mark = PlatformMark(a.Platform);
            if (mark != null)
            {
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(mark, new Rectangle(bx + Ui.X(10), by, Ui.X(14), Ui.X(14)));
                g.InterpolationMode = old;
                tx = bx + Ui.X(32);
            }
            Col(g, a.Id, Ui.Text, tx, by, Ui.Mono(9.5f));
            Col(g, a.Platform,
                a.Platform == "unknown" || a.Platform.EndsWith("?") ? Ui.Faint : Ui.Accent,
                bx + Ui.X(240), by);
            Col(g, string.Format(T("list.files"), a.FileCount), sel ? Ui.Text : Ui.Dim, bx + Ui.X(430), by);
            if (a.LockedCount > 0)
                Col(g, string.Format(T("list.locked"), a.LockedCount), sel ? Ui.Text : Ui.Faint, bx + Ui.X(560), by);
        }

        static void Col(Graphics g, string s, Color c, int x, int y, Font f = null) =>
            TextRenderer.DrawText(g, s, f ?? Ui.Body, new Point(x, y), c, TextFormatFlags.NoPadding);
    }
}
