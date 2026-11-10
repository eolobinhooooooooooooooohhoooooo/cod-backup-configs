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
        const int W = 980; // initial design width; live width comes from ClientSize
        const int GAP = 12;

        const int PANEL_W = (W - 2 * M - GAP) / 2;
        const int PANEL_H = 166;
        const int PAD = 14;
        const int HH = 44; // integrated header band, design px
        const int CARD_H = 122;
        const int BTN_Y = 134; // action button row, panel-local

        ToolTip _tt;
        Label _secGame, _secAccounts, _secBackups;
        Label _status;
        FlagButton _btnEn, _btnPt;
        WindowButton _btnMin, _btnClose;
        Bitmap _hdrIcon;
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
        int _panelW; // live device-px panel width, refreshed in ApplyLayout
        int _lastW;  // resize guard so relayout cannot loop with itself
        System.Windows.Forms.Timer _settle; // startup resync, see StartSettle

        List<GameInfo> _games;
        int _selGame = -1;
        List<Account> _accounts = new();
        List<Snapshot> _snaps = new();

        static string T(string key) => L10n.T(key);

        // platform marks per guessed platform, cached after first load
        static readonly Dictionary<string, Bitmap> _marks = new(StringComparer.OrdinalIgnoreCase);

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
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.X(W), Ui.X(600 + HH));
            MinimumSize = new Size(Ui.X(920), Ui.X(620));
            DoubleBuffered = true;

            // taskbar/alt-tab icon straight from the multi-frame kit ico;
            // never a GetHicon() rasterized down from the 256 png
            var icoStream = typeof(MainForm).Assembly.GetManifestResourceStream("img/icon.ico");
            if (icoStream != null)
            {
                Icon = new Icon(icoStream);
                icoStream.Dispose();
            }

            _tt = new ToolTip { AutoPopDelay = 10000, InitialDelay = 250, ShowAlways = true };
            _games = CodPaths.Games;

            BuildTopBar();
            BuildGameSection();
            BuildAccountsSection();
            BuildBackupsSection();
            BuildFooter();
            ApplyLayout();
            _lastW = ClientSize.Width;

            Load += (s, e) =>
            {
                Prefs.Load();
                L10n.Lang = Prefs.Lang;
                ApplyTexts();
                int i = _games.FindIndex(g => g.Key == Prefs.LastGame);
                SelectGame(i < 0 ? 0 : i);
            };
            FormClosed += (s, e) => { if (_settle != null) { _settle.Stop(); _settle.Dispose(); _settle = null; } Prefs.Save(); };
            Shown += (s, e) => StartSettle();
        }

        // film grain background; backcolor stays flat for resize-flash protection
        protected override void OnPaintBackground(PaintEventArgs e) =>
            e.Graphics.FillRectangle(GrainFx.Brush, ClientRectangle);

        // frameless chrome: 1px outer frame, header band with icon, title
        // and version badge. child controls paint on top of the band, and
        // the frame draws over everything else in the client area
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int hh = Ui.X(HH);
            using (var b = new SolidBrush(Ui.Header)) g.FillRectangle(b, 0, 0, Width, hh);
            using (var p = new Pen(Ui.Line))
            {
                g.DrawLine(p, 0, hh, Width - 1, hh);
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }

            if (_hdrIcon == null) _hdrIcon = Assets.Img("icon-256.png");
            if (_hdrIcon != null)
            {
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_hdrIcon, new Rectangle(Ui.X(M), (hh - Ui.X(20)) / 2, Ui.X(20), Ui.X(20)));
                g.InterpolationMode = old;
            }

            int tx = Ui.X(M + 30);
            string ver = "v" + typeof(MainForm).Assembly.GetName().Version.ToString(3);
            TextRenderer.DrawText(g, T("title"), Ui.Title,
                new Rectangle(tx, 0, Width - tx - Ui.X(280), hh), Ui.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var tw = TextRenderer.MeasureText(g, T("title"), Ui.Title, Size.Empty, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, ver, Ui.Small,
                new Rectangle(tx + (int)tw.Width + Ui.X(10), 0, Ui.X(90), hh), Ui.Faint,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // dragging the header moves the window the way a native caption would
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y <= Ui.X(HH))
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        const int WM_NCHITTEST = 0x0084;

        // frameless edge/corner hit targets so the window resizes like a
        // normal chrome one; a 5px band around the client edge
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != WM_NCHITTEST || (int)m.Result != 1) return; // htclient
            int b = Ui.X(5);
            Point p = PointToClient(new Point(
                (short)(m.LParam.ToInt32() & 0xffff),
                (short)(m.LParam.ToInt32() >> 16)));
            int code = 1;
            if (p.X <= b && p.Y <= b) code = 14;                // httopleft
            else if (p.X >= Width - b && p.Y <= b) code = 15;   // httopright
            else if (p.X <= b && p.Y >= Height - b) code = 16;  // htbodyleft
            else if (p.X >= Width - b && p.Y >= Height - b) code = 17; // htbodyright
            else if (p.X <= b) code = 10;                       // htleft
            else if (p.X >= Width - b) code = 12;                // htright
            else if (p.Y <= b) code = 11;                        // httop
            else if (p.Y >= Height - b) code = 13;               // httbottom
            m.Result = (IntPtr)code;
        }

        // reflow whenever the user drags an edge; vertical stretching stays
        // owned by LayoutBottom so the list keeps breathing with its rows
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            if (_listBorder == null || ClientSize.Width == _lastW) return;
            _lastW = ClientSize.Width;
            ApplyLayout();
        }

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // immersive dark mode for the system menus (taskbar popup, alt-space)
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
            _lastW = 0;
            ApplyLayout();
        }

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int l, t, r, b; }
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT rc);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr m, int type, out ushort x, out ushort y);

        // windows can rebaseline a window's dpi shortly after show without
        // delivering any size message (display reconfigured mid-session). the
        // window keeps a stale dpi association while its monitor has moved on,
        // so the layout drawn at the old scale no longer fits the screen.
        // poll briefly after show and resync to reality through either of two
        // channels: the true client rect disagreeing with winforms' cache, or
        // the window's own dpi diverging from its monitor's dpi
        void StartSettle()
        {
            int ticks = 0, dpiMiss = 0;
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 250 };
            t.Tick += (s, e) =>
            {
                ticks++;
                if (ticks > 32) { StopSettle(t); return; }

                RECT rc;
                if (GetClientRect(Handle, out rc))
                {
                    int w = rc.r - rc.l, h = rc.b - rc.t;
                    if (w > 100 && h > 100 &&
                        (Math.Abs(w - ClientSize.Width) > 2 || Math.Abs(h - ClientSize.Height) > 2))
                    {
                        Ui.F = w / (float)W;
                        _lastW = 0;
                        ClientSize = new Size(w, h);
                        ApplyLayout();
                        StopSettle(t);
                        return;
                    }
                }

                if (ticks < 4) return; // let startup itself finish first
                int mdi = MonitorDpi();
                if (mdi > 0 && Math.Abs(mdi / 96f - Ui.F) > 0.02f)
                {
                    if (++dpiMiss >= 2)
                    {
                        float f = mdi / 96f;
                        int nh = (int)Math.Round(ClientSize.Height / Ui.F * f);
                        Ui.F = f;
                        _lastW = 0;
                        ClientSize = new Size(Ui.X(W), nh);
                        ApplyLayout();
                        StopSettle(t);
                    }
                    return;
                }
                dpiMiss = 0;
            };
            _settle = t;
            t.Start();
        }

        // effective dpi of the monitor under this window, 0 on failure
        int MonitorDpi()
        {
            try
            {
                IntPtr m = MonitorFromWindow(Handle, 2); // MONITOR_DEFAULTTONEAREST
                if (m == IntPtr.Zero) return 0;
                ushort x, y;
                return GetDpiForMonitor(m, 0, out x, out y) == 0 ? (int)x : 0;
            }
            catch { return 0; }
        }

        void StopSettle(System.Windows.Forms.Timer t)
        {
            t.Stop();
            t.Dispose();
            if (ReferenceEquals(_settle, t)) _settle = null;
        }

        // frameless keeps sysmenu + minimizebox so the taskbar context menu
        // (restore / minimize / close) keeps working
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_SYSMENU = 0x00080000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_SYSMENU;
                return cp;
            }
        }

        // -------------------------------------------------------------- layout

        void BuildTopBar()
        {
            // the band itself is painted in OnPaint; only the interactive
            // parts are controls, all sitting in the header zone
            _btnEn = new FlagButton
            {
                Width = 60, Height = 28, Text = "EN",
                Location = new Point(W - M - 228, 8),
                Font = Ui.Small,
                Flag = Assets.Img("flag-en.png"),
            };
            _btnEn.Click += (s, e) => SetLang("en");

            _btnPt = new FlagButton
            {
                Width = 60, Height = 28, Text = "PT",
                Location = new Point(W - M - 160, 8),
                Font = Ui.Small,
                Flag = Assets.Img("flag-pt.png"),
            };
            _btnPt.Click += (s, e) => SetLang("pt");

            _btnMin = new WindowButton(WindowButton.Kind.Minimize)
            {
                Location = new Point(W - M - 88, 0),
                Size = new Size(44, HH),
            };

            _btnClose = new WindowButton(WindowButton.Kind.Close)
            {
                Location = new Point(W - M - 44, 0),
                Size = new Size(44, HH),
            };

            Controls.Add(_btnEn);
            Controls.Add(_btnPt);
            Controls.Add(_btnMin);
            Controls.Add(_btnClose);
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
                    Size = new Size(cw, CARD_H),
                };
                _cards[i].Clicked += () => SelectGame(idx);
                _cards[i].BrowseClick += BrowseFor;
                Controls.Add(_cards[i]);
            }
        }

        void BuildAccountsSection()
        {
            _secAccounts = new SectionLabel { Location = new Point(M, 210) };
            Controls.Add(_secAccounts);

            _btnRefresh = new LineButton
            {
                Font = Ui.Btn,
                Location = new Point(W - M - 88, 204),
                Size = new Size(88, 26),
            };
            _btnRefresh.Icon = Assets.Img("ic-refresh.png");
            _btnRefresh.Click += (s, e) => { Rescan(); Status(T("st.ready")); };
            Controls.Add(_btnRefresh);

            _listBorder = new BorderPanel
            {
                Location = new Point(M, 230),
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
            _secBackups = new SectionLabel { Location = new Point(M, 356) };
            Controls.Add(_secBackups);

            // left box: saving a backup of the selected account
            _savePanel = ActionPanel(new Point(M, 374));
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
                AutoSize = false, AutoEllipsis = true,
                Font = Ui.Small, ForeColor = Ui.Faint,
                BackColor = Color.Transparent, Location = new Point(PAD, 90),
            };
            _savePanel.Controls.Add(_saveNote);
            // block A: name + backup + lock live in this panel, side by side
            _btnBackup = PanelButton(_savePanel, DoBackup, new Point(PAD, BTN_Y), new Size(200, 28));
            _btnBackup.Icon = Assets.Img("ic-backup.png");
            _btnLock = PanelButton(_savePanel, DoLock, new Point(PAD + 208, BTN_Y), new Size(204, 28));
            _btnLock.Icon = Assets.Img("ic-lock.png");

            // right box: restoring an earlier snapshot
            _restorePanel = ActionPanel(new Point(M + PANEL_W + GAP, 374));
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
                Location = new Point(PAD, 112), Size = new Size(PANEL_W - 2 * PAD, 16),
            };
            _restorePanel.Controls.Add(_chkReplace);
            _btnRestore = PanelButton(_restorePanel, DoRestore, new Point(PAD, BTN_Y), new Size(PANEL_W - 2 * PAD, 28));
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
                AutoSize = false, AutoEllipsis = true,
                Font = Ui.Small, ForeColor = Ui.Dim,
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
                AutoSize = false, AutoEllipsis = true,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Color.Transparent,
                Location = new Point(M, 608),
            };
            Controls.Add(_status);
        }

        // fit a line button to its current label: text + icon slot when
        // present + at least 24px of horizontal breathing room; re-measured
        // on every language switch because en/pt labels differ in width
        static int FitWidth(Control b)
        {
            var t = TextRenderer.MeasureText(b.Text, b.Font, Size.Empty, TextFormatFlags.NoPadding);
            bool icon = b is LineButton lb && lb.Icon != null;
            return Math.Max(Ui.X(64), t.Width + (icon ? Ui.X(48) : Ui.X(28)));
        }

        // positions every control for the current client size + Ui.F; runs
        // from the constructor, on language switches, on dpi changes and on
        // edge resizes. widths derive from the live client size, not from W,
        // so dragging an edge reflows the whole layout
        void ApplyLayout()
        {
            // floor to the live client width: a dpi transition can hand us a
            // narrower window than 920*scale and the controls must fit it
            int cw = ClientSize.Width;
            int m = Ui.X(M), gap = Ui.X(GAP);
            _panelW = (cw - 2 * m - gap) / 2;
            int cardW = (cw - 2 * m - 2 * gap) / 3;

            // header cluster, right-anchored so it survives any width
            _btnClose.Location = new Point(cw - Ui.X(44), 0);
            _btnClose.Size = new Size(Ui.X(44), Ui.X(HH));
            _btnMin.Location = new Point(cw - Ui.X(88), 0);
            _btnMin.Size = new Size(Ui.X(44), Ui.X(HH));
            _btnPt.Location = new Point(cw - m - Ui.X(160), Ui.X(8));
            _btnPt.Size = new Size(Ui.X(60), Ui.X(28));
            _btnEn.Location = new Point(cw - m - Ui.X(228), Ui.X(8));
            _btnEn.Size = new Size(Ui.X(60), Ui.X(28));

            _secGame.Location = new Point(m, Ui.X(56 + HH));
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].Location = new Point(m + i * (cardW + gap), Ui.X(74 + HH));
                _cards[i].Size = new Size(cardW, Ui.X(CARD_H));
            }

            _secAccounts.Location = new Point(m, Ui.X(210 + HH));
            _btnRefresh.Size = new Size(FitWidth(_btnRefresh), Ui.X(26));
            _btnRefresh.Location = new Point(cw - m - _btnRefresh.Width, Ui.X(204 + HH));
            _listBorder.Location = new Point(m, Ui.X(230 + HH));
            _listBorder.Size = new Size(cw - 2 * m, Ui.X(116));

            // everything below the list is vertical-only and lives in
            // LayoutBottom; size the panels here so its x math has the live
            // panel width to work with
            _savePanel.Size = new Size(_panelW, Ui.X(PANEL_H));
            _restorePanel.Size = new Size(_panelW, Ui.X(PANEL_H));
            LayoutPanels();
            LayoutBottom();
        }

        // children of the two action panels, in panel coordinates. hint and
        // note labels span the full panel width with ellipsis, so no text
        // overflows its box in either language
        void LayoutPanels()
        {
            int pad = Ui.X(PAD);
            int avail = _panelW - 2 * pad;
            int gap = Ui.X(8);

            _saveTag.Location = new Point(pad, Ui.X(12));
            _saveHint.Location = new Point(pad, Ui.X(30));
            _saveHint.Size = new Size(avail, Ui.X(14));
            _txtName.Location = new Point(pad, Ui.X(50));
            _txtName.Size = new Size(avail, Ui.X(26));
            _saveNote.Location = new Point(pad, Ui.X(90));
            _saveNote.Size = new Size(avail, Ui.X(14));

            // backup + lock fit their labels; when both together would spill
            // past the panel they fall back to even 50/50 columns
            int wB = FitWidth(_btnBackup), wL = FitWidth(_btnLock);
            if (wB + wL + gap > avail) { wB = (avail - gap) / 2; wL = avail - gap - wB; }
            _btnBackup.Location = new Point(pad, Ui.X(BTN_Y));
            _btnBackup.Size = new Size(wB, Ui.X(28));
            _btnLock.Location = new Point(pad + wB + gap, Ui.X(BTN_Y));
            _btnLock.Size = new Size(wL, Ui.X(28));

            _restoreTag.Location = new Point(pad, Ui.X(12));
            _restoreHint.Location = new Point(pad, Ui.X(30));
            _restoreHint.Size = new Size(avail, Ui.X(14));
            _cbSnap.Location = new Point(pad, Ui.X(50));
            _cbSnap.Size = new Size(avail, Ui.X(26));
            _txtTarget.Location = new Point(pad, Ui.X(84));
            _txtTarget.Size = new Size(avail, Ui.X(26));
            _chkReplace.Location = new Point(pad, Ui.X(112));
            _chkReplace.Size = new Size(avail, Ui.X(16));
            _btnRestore.Location = new Point(pad, Ui.X(BTN_Y));
            _btnRestore.Size = new Size(avail, Ui.X(28));
        }

        // stretch or shrink the window so the list only takes as much
        // space as it has accounts in it, then re-anchor the backups
        // section, folder row and footer underneath it
        void LayoutBottom()
        {
            int m = Ui.X(M), gap = Ui.X(GAP);
            int rowH = Ui.X(26);
            int rows = Math.Min(Math.Max(_accounts.Count, 1), 4);
            _lb.ItemHeight = rowH;
            _listBorder.Height = rows * rowH + Ui.X(8);

            int secY = _listBorder.Bottom + Ui.X(14);
            _secBackups.Location = new Point(m, secY);

            int pY = secY + Ui.X(18);
            _savePanel.Location = new Point(m, pY);
            _restorePanel.Location = new Point(m + _panelW + gap, pY);

            int uY = pY + _savePanel.Height + Ui.X(14);
            // folder row: buttons fit their labels, 50/50 when both spill
            int avail = ClientSize.Width - 2 * m;
            int wP = FitWidth(_btnPlayers), wK = FitWidth(_btnBackups);
            if (wP + wK + gap > avail) { wP = (avail - gap) / 2; wK = avail - gap - wP; }
            _btnPlayers.Location = new Point(m, uY);
            _btnPlayers.Size = new Size(wP, Ui.X(28));
            _btnBackups.Location = new Point(m + wP + gap, uY);
            _btnBackups.Size = new Size(wK, Ui.X(28));

            int lY = uY + Ui.X(28) + Ui.X(12);
            _footerLine.Location = new Point(m, lY);
            _footerLine.Width = ClientSize.Width - 2 * m;
            _status.Location = new Point(m, lY + Ui.X(12));
            _status.Size = new Size(ClientSize.Width - 2 * m, Ui.X(16));

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
            _tt.SetToolTip(_btnMin, T("tip.min"));
            _tt.SetToolTip(_btnClose, T("tip.close"));
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

            // labels changed (en/pt): re-fit every button and label to the
            // new text before any of it can clip
            ApplyLayout();
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
