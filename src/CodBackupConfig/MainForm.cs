using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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

        Dictionary<Control, string> _tips;    // l10n tip key per control
        HashSet<Control> _tipHooked;
        Control _tipHot;
        TipPopup _tipPop;
        System.Windows.Forms.Timer _tipTimer;
        DateTime _tipShown;
        IntroOverlay _intro;
        Label _secGame, _secAccounts, _secBackups;
        Label _status;
        FlagButton _btnEn, _btnPt;
        WindowButton _btnMin, _btnMax, _btnClose;
        Icon _hdrIco;
        GameCard[] _cards;
        LineButton _btnRefresh, _btnBackup, _btnRestore, _btnLock, _btnPlayers, _btnBackups;
        TickBox _chkReplace;
        TextBox _txtName;
        ChoiceBox _cbSnap, _cbTarget;
        BorderPanel _listBorder, _savePanel, _restorePanel;
        Label _saveTag, _saveHint, _saveNote, _restoreTag, _restoreHint, _lblTo;
        Panel _footerLine;
        DarkListBox _lb;
        int _lbPrevSel = -1;
        Size _lastSz; // resize guard so relayout cannot loop with itself
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

        // honest scale of the monitor this window sits on; never derived
        // from the client width, which doubled the whole layout when the
        // window came up wider than the design size maximized
        float RealDpiScale()
        {
            try
            {
                uint d = GetDpiForWindow(Handle);
                if (d >= 96) return d / 96f;
            }
            catch { }
            int mdi = MonitorDpi();
            if (mdi >= 96) return mdi / 96f;
            return DpiScale();
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
            MaximizeBox = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.X(W), Ui.X(688));
            MinimumSize = new Size(Ui.X(920), Ui.X(688));
            // opens maximized; the client size set above stays the restore
            // bounds, so restoring lands back on the design window
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;

            // taskbar/alt-tab icon and the header mark straight from the
            // multi-frame kit ico; never a GetHicon() rasterized down
            var icoStream = typeof(MainForm).Assembly.GetManifestResourceStream("img/icon.ico");
            if (icoStream != null)
            {
                Icon = new Icon(icoStream);
                _hdrIco = (Icon)Icon.Clone();
                icoStream.Dispose();
            }

            _tips = new Dictionary<Control, string>();
            _tipHooked = new HashSet<Control>();
            _tipTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _tipTimer.Tick += TipTick;
            Deactivate += (s, e) => HideTip();
            _games = CodPaths.Games;

            BuildTopBar();
            BuildGameSection();
            BuildAccountsSection();
            BuildBackupsSection();
            BuildFooter();
            ApplyLayout();
            _lastSz = ClientSize;

            Load += (s, e) =>
            {
                Prefs.Load();
                L10n.Lang = Prefs.Lang;
                ApplyTexts();
                int i = _games.FindIndex(g => g.Key == Prefs.LastGame && g.Available);
                if (i < 0) i = _games.FindIndex(g => g.Available);
                SelectGame(i);
            };
            FormClosed += (s, e) =>
            {
                if (_settle != null) { _settle.Stop(); _settle.Dispose(); _settle = null; }
                if (_tipTimer != null) { _tipTimer.Stop(); _tipTimer.Dispose(); _tipTimer = null; }
                HideTip();
                if (_intro != null) _intro.Skip();
                Prefs.Save();
            };
            Shown += (s, e) => { StartIntro(); StartSettle(); };
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

            if (_hdrIco != null)
                g.DrawIcon(_hdrIco, new Rectangle(Ui.X(M), (hh - Ui.X(20)) / 2, Ui.X(20), Ui.X(20)));

            int tx = Ui.X(M + 30);
            string ver = "v" + typeof(MainForm).Assembly.GetName().Version.ToString(3);
            // right cluster (flags + 3 window buttons) owns the far side of
            // the band; the title may never reach it
            TextRenderer.DrawText(g, T("title"), Ui.Title,
                new Rectangle(tx, 0, Width - tx - Ui.X(330), hh), Ui.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var tw = TextRenderer.MeasureText(g, T("title"), Ui.Title, Size.Empty, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, ver, Ui.Small,
                new Rectangle(tx + (int)tw.Width + Ui.X(10), 0, Ui.X(90), hh), Ui.Faint,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;
        const int WM_CAPTURECHANGED = 0x0210;
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern bool SetCapture(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // manual edge/corner resize: in this borderless PMv2 setup the
        // DefWindowProc sizing loop never starts from HT* pixels, so we drive
        // the bounds ourselves while WM_NCHITTEST supplies the cursor shapes
        int _rzCode = 1;
        Rectangle _rzBounds;
        Point _rzMouse;
        Size _rzMax;

        // hit code for a client point; 1 = htclient, 10..17 = the bands
        int HitCode(Point p)
        {
            int b = Ui.X(6);
            if (p.X <= b && p.Y <= b) return 13;                 // httopleft
            if (p.X >= Width - b && p.Y <= b) return 14;         // httopright
            if (p.X <= b && p.Y >= Height - b) return 16;        // htbottomleft
            if (p.X >= Width - b && p.Y >= Height - b) return 17; // htbottomright
            if (p.X <= b) return 10;                             // htleft
            if (p.X >= Width - b) return 11;                     // htright
            if (p.Y <= b) return 12;                             // httop
            if (p.Y >= Height - b) return 15;                    // htbottom
            return 1;                                            // htclient
        }

        // does a live descendant cover this screen point? the header
        // buttons sit flush in the top-right corner and must keep their
        // own hit area over the resize bands
        static bool ChildUnder(Control root, Point sp)
        {
            foreach (Control c in root.Controls)
            {
                if (!c.Visible || c.IsDisposed) continue;
                try
                {
                    if (c.RectangleToScreen(c.ClientRectangle).Contains(sp)) return true;
                }
                catch { }
            }
            return false;
        }

        // begin a tracked resize from a band press; sp is the press point in
        // screen coordinates, taken from WM_NCLBUTTONDOWN's lParam
        void BeginResize(int code, Point sp)
        {
            RECT wa = WorkRect();
            _rzCode = code;
            _rzBounds = Bounds;
            _rzMouse = sp;
            _rzMax = new Size(wa.r - wa.l, wa.b - wa.t);
        }

        // dragging the header moves the window the way a native caption would;
        // band presses never land here (they arrive as WM_NCLBUTTONDOWN)
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || e.Y > Ui.X(HH)) return;
            if (e.Clicks == 2) // double press toggles maximize, native caption way
            {
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                return;
            }
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_rzCode < 10 || _rzCode > 17) return;
            Point now = PointToScreen(e.Location);
            int dx = now.X - _rzMouse.X, dy = now.Y - _rzMouse.Y;
            int minW = MinimumSize.Width, minH = MinimumSize.Height;
            var r = _rzBounds;
            if (_rzCode == 10 || _rzCode == 13 || _rzCode == 16)
            {
                int nl = _rzBounds.Left + dx, nw = _rzBounds.Width - dx;
                if (nw < minW) { nw = minW; nl = _rzBounds.Right - minW; }
                if (nw > _rzMax.Width) { nw = _rzMax.Width; nl = _rzBounds.Left; }
                r.X = nl; r.Width = nw;
            }
            if (_rzCode == 11 || _rzCode == 14 || _rzCode == 17)
            {
                int nw = _rzBounds.Width + dx;
                r.Width = nw < minW ? minW : (nw > _rzMax.Width ? _rzMax.Width : nw);
            }
            if (_rzCode == 12 || _rzCode == 13 || _rzCode == 14)
            {
                int nt = _rzBounds.Top + dy, nh = _rzBounds.Height - dy;
                if (nh < minH) { nh = minH; nt = _rzBounds.Bottom - minH; }
                if (nh > _rzMax.Height) { nh = _rzMax.Height; nt = _rzBounds.Top; }
                r.Y = nt; r.Height = nh;
            }
            if (_rzCode == 15 || _rzCode == 16 || _rzCode == 17)
            {
                int nh = _rzBounds.Height + dy;
                r.Height = nh < minH ? minH : (nh > _rzMax.Height ? _rzMax.Height : nh);
            }
            if (r != _rzBounds) Bounds = r;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_rzCode >= 10 && _rzCode <= 17)
            {
                _rzCode = 1;
                Capture = false;
            }
        }

        const int WM_NCHITTEST = 0x0084;
        const int WM_GETMINMAXINFO = 0x0024;

        [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] struct MINMAXINFO
        {
            public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
        }
        [StructLayout(LayoutKind.Sequential)] struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr h, ref MONITORINFO mi);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint f);

        // working area of the monitor that owns the window, physical pixels
        // (this process is PerMonitorV2, so raw user32 rects are physical)
        RECT WorkRect()
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            IntPtr mon = MonitorFromWindow(Handle, 2);
            if (mon != IntPtr.Zero && GetMonitorInfo(mon, ref mi) && (mi.rcWork.l != 0 || mi.rcWork.t != 0 || mi.rcWork.r != 0 || mi.rcWork.b != 0))
                return mi.rcWork;
            var r = Screen.PrimaryScreen.WorkingArea;
            return new RECT { l = r.Left, t = r.Top, r = r.Right, b = r.Bottom };
        }

        // keep the maximized rect pinned to the work area of whatever monitor
        // owns the window; refreshed on handle creation, moves and dpi
        // changes so crossing monitor boundaries or scaling never leaves the
        // frame overhanging the screen or hiding under the taskbar
        void UpdateMaximizedBounds()
        {
            try
            {
                RECT wa = WorkRect();
                MaximizedBounds = new Rectangle(wa.l, wa.t, wa.r - wa.l, wa.b - wa.t);
            }
            catch { }
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            UpdateMaximizedBounds();
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            UpdateMaximizedBounds();
        }

        // frameless edge/corner hit targets so the window resizes like a
        // normal chrome one; a 6px band around the client edge. when
        // maximized the max position/size are pinned to the work area of
        // whatever monitor owns the window, so the frame never overhangs
        // the screen or hides under the taskbar
        protected override void WndProc(ref Message m)
        {
            // a band press arrives as WM_NCLBUTTONDOWN (NCHITTEST says HT*),
            // which DefWindowProc's sizing loop would swallow; start our own
            // tracked resize instead so the bounds flow through Relayout
            if (m.Msg == WM_NCLBUTTONDOWN)
            {
                int hc = m.WParam.ToInt32();
                if (hc >= 10 && hc <= 17 && WindowState == FormWindowState.Normal)
                {
                    BeginResize(hc, new Point(
                        (short)(m.LParam.ToInt32() & 0xffff),
                        (short)(m.LParam.ToInt32() >> 16)));
                    SetCapture(Handle);
                    return;
                }
            }
            if (m.Msg == WM_CAPTURECHANGED && _rzCode >= 10 && _rzCode <= 17
                && (long)m.WParam == (long)Handle)
                _rzCode = 1; // capture lost without button up (alt-tab, menu)
            base.WndProc(ref m);
            if (m.Msg == WM_GETMINMAXINFO)
            {
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
                RECT wa = WorkRect();
                // ptMaxPosition is left at DefWindowProc's prefill: on PMv2
                // windows the OS treats an overwrite as a delta from that
                // prefill, which doubled the offset on the left monitor
                mmi.ptMaxSize.x = wa.r - wa.l;
                mmi.ptMaxSize.y = wa.b - wa.t;
                mmi.ptMaxTrackSize.x = wa.r - wa.l;
                mmi.ptMaxTrackSize.y = wa.b - wa.t;
                Marshal.StructureToPtr(mmi, m.LParam, false);
                return;
            }
            if (m.Msg == 0x0112 && (m.WParam.ToInt32() & 0xFFF0) == 0xF030)
            {
                // PMv2 quirk: DefWindowProc can land the maximized window one
                // work-area width off on a non-primary monitor. The position
                // correction is done here, in the window's own (physical)
                // coordinate space, right after DefWindowProc maximizes it.
                RECT wa = WorkRect();
                SetWindowPos(Handle, IntPtr.Zero, wa.l, wa.t, wa.r - wa.l, wa.b - wa.t, 0x0004 | 0x0010);
            }
            if (m.Msg != WM_NCHITTEST || (int)m.Result != 1) return; // htclient
            if (WindowState == FormWindowState.Maximized) return;   // no bands while maximized
            Point p = PointToClient(new Point(
                (short)(m.LParam.ToInt32() & 0xffff),
                (short)(m.LParam.ToInt32() >> 16)));
            // a visible child owns this pixel (the header buttons sit flush
            // in the top-right corner): the bands yield to it, or the
            // close button's outermost strip would start a resize instead
            // of closing
            if (ChildUnder(this, PointToScreen(p))) return;
            m.Result = (IntPtr)HitCode(p);
        }

        // reflow on every width OR height change: edge drags, maximize and
        // restore all flow through WM_SIZE; the cards and list split the
        // vertical slack
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            if (_listBorder == null || ClientSize == _lastSz) return;
            _lastSz = ClientSize;
            SuspendLayout();
            try
            {
                _cbSnap.Dismiss(); // an open dropdown cannot outlive its spot
                ApplyLayout();
            }
            finally { ResumeLayout(); }
        }

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateMaximizedBounds();
            // immersive dark mode for the system menus (taskbar popup, alt-space)
            try
            {
                int one = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref one, 4) != 0) // Win10 20H1+ / Win11
                    DwmSetWindowAttribute(Handle, 19, ref one, 4);      // older builds
            }
            catch { }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            // winforms rescales MinimumSize per axis from the ratio of the
            // suggested and current bounds, which goes non-uniform whenever the
            // window was clamped during the transition; reassert the design
            // minimum and rebuild the layout at the new scale so the clamp
            // floor stays exact
            Ui.F = DpiScale();
            MinimumSize = new Size(Ui.X(920), Ui.X(688));
            _lastSz = Size.Empty;
            UpdateMaximizedBounds();
            ApplyLayout();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            // uniform rescale of every control, then rebuild the dynamic part
            Ui.F = DpiScale();
            _lastSz = Size.Empty;
            UpdateMaximizedBounds();
            ApplyLayout();
        }

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int l, t, r, b; }
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT rc);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr m, int type, out ushort x, out ushort y);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);

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
                        // resync to device reality at the true monitor scale;
                        // the factor never comes from the width, that doubled
                        // the layout when startup landed wider than design
                        Ui.F = RealDpiScale();
                        _lastSz = Size.Empty;
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
                        if (WindowState == FormWindowState.Maximized)
                        {
                            // the bounds are the work area's; only the scale
                            // changed, so relayout in place
                            Ui.F = f;
                            _lastSz = Size.Empty;
                            ApplyLayout();
                        }
                        else
                        {
                            int nh = (int)Math.Round(ClientSize.Height / Ui.F * f);
                            Ui.F = f;
                            _lastSz = Size.Empty;
                            ClientSize = new Size(ClientSize.Width, nh);
                            ApplyLayout();
                        }
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

        // frameless keeps sysmenu + minimizebox + maximizebox so the taskbar
        // context menu (restore / minimize / maximize / close) keeps working
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_SYSMENU = 0x00080000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU;
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
                Location = new Point(W - M - 248, 8),
                Font = Ui.Small,
                Flag = Assets.Img("flag-en.png"),
            };
            _btnEn.Click += (s, e) => SetLang("en");

            _btnPt = new FlagButton
            {
                Width = 60, Height = 28, Text = "PT",
                Location = new Point(W - M - 180, 8),
                Font = Ui.Small,
                Flag = Assets.Img("flag-pt.png"),
            };
            _btnPt.Click += (s, e) => SetLang("pt");

            _btnMin = new WindowButton(WindowButton.Kind.Minimize)
            {
                Location = new Point(W - M - 132, 0),
                Size = new Size(44, HH),
            };

            _btnMax = new WindowButton(WindowButton.Kind.Maximize)
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
            Controls.Add(_btnMax);
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
            _lb = new DarkListBox
            {
                Dock = DockStyle.Fill,
                RowHeight = 26,
                RowPainter = Lb_PaintRow,
            };
            _lb.SelectedIndexChanged += (s, e) =>
            {
                int cur = _lb.SelectedIndex;
                int prev = _lbPrevSel;
                _lbPrevSel = cur;
                // the framework occasionally skips the deselect repaint of the
                // previous row, leaving a ghost highlight until full activation
                if (prev >= 0 && prev != cur) _lb.Invalidate();
                var a = SelAcct();
                SelectTarget(a?.Id);
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
                // the preset's source becomes the destination by default
                if (i >= 0 && i < _snaps.Count)
                    SelectTarget(_snaps[i].AccountId);
            };
            _restorePanel.Controls.Add(_cbSnap);
            int tW = PANEL_W - 2 * PAD - 210 - 8;
            _lblTo = new Label
            {
                AutoSize = false, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleRight,
                Font = Ui.Small, ForeColor = Ui.Faint, BackColor = Color.Transparent,
                Location = new Point(PAD, 90), Size = new Size(32, 14),
            };
            _restorePanel.Controls.Add(_lblTo);
            _cbTarget = new ChoiceBox
            {
                Location = new Point(PAD + 36, 84), Size = new Size(tW - 36, 26),
                Font = Ui.Body,
            };
            _restorePanel.Controls.Add(_cbTarget);
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
        // from the constructor, on language switches, on dpi changes, on
        // list changes and on every resize. widths derive from the live
        // client size, so dragging any edge reflows the whole layout; extra
        // height splits between the game cards and the account list and the
        // window size is never rewritten here
        void ApplyLayout()
        {
            int cw = ClientSize.Width, ch = ClientSize.Height;
            int m = Ui.X(M), gap = Ui.X(GAP), hh = Ui.X(HH);
            int panelW = (cw - 2 * m - gap) / 2;
            int cardW = (cw - 2 * m - 2 * gap) / 3;

            // header cluster, right-anchored so it survives any width; the
            // maximize glyph depends on window state, refresh it each pass
            _btnMax.Invalidate();
            _btnClose.Location = new Point(cw - Ui.X(44), 0);
            _btnClose.Size = new Size(Ui.X(44), hh);
            _btnMax.Location = new Point(cw - Ui.X(88), 0);
            _btnMax.Size = new Size(Ui.X(44), hh);
            _btnMin.Location = new Point(cw - Ui.X(132), 0);
            _btnMin.Size = new Size(Ui.X(44), hh);
            _btnPt.Location = new Point(cw - m - Ui.X(180), Ui.X(8));
            _btnPt.Size = new Size(Ui.X(60), Ui.X(28));
            _btnEn.Location = new Point(cw - m - Ui.X(248), Ui.X(8));
            _btnEn.Size = new Size(Ui.X(60), Ui.X(28));

            // extra height above the design minimum splits: a quarter grows
            // the game cards toward their 200px cap, the rest lengthens the
            // account list, so a maximized window has no dead gap
            int below = Ui.X(14 + 18 + PANEL_H + 14 + 28 + 12 + 12 + 16 + 14);
            int minListH = Ui.X(4 * 26 + 8);
            int extra = ch - (Ui.X(230 + HH) + minListH + below);
            int cardBonus = extra > 0 ? Math.Min((int)Math.Round(extra * 0.25f), Ui.X(200 - CARD_H)) : 0;
            int cardH = Ui.X(CARD_H) + cardBonus;
            int cardsY = Ui.X(74 + HH);
            int secAccY = cardsY + cardH + Ui.X(14);
            int listTop = secAccY + Ui.X(26); // floor under the refresh row

            _secGame.Location = new Point(m, Ui.X(56 + HH));
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].Location = new Point(m + i * (cardW + gap), cardsY);
                _cards[i].Size = new Size(cardW, cardH);
            }
            _cards[2].Width = (cw - m) - _cards[2].Left; // flush right edge

            _secAccounts.Location = new Point(m, secAccY);
            int refW = FitWidth(_btnRefresh);
            _btnRefresh.Size = new Size(refW, Ui.X(26));
            _btnRefresh.Location = new Point(cw - m - refW, secAccY - Ui.X(6));

            // the list takes whatever vertical space is left between its
            // top and the fixed stack below it (section label, action
            // panels, folder row, footer line, status, bottom pad)
            int listH = Math.Max(minListH, ch - listTop - below);
            _lb.RowHeight = Ui.X(26);
            _listBorder.Location = new Point(m, listTop);
            _listBorder.Size = new Size(cw - 2 * m, listH);

            int secY = listTop + listH + Ui.X(14);
            _secBackups.Location = new Point(m, secY);

            int pY = secY + Ui.X(18);
            _savePanel.Location = new Point(m, pY);
            _savePanel.Size = new Size(panelW, Ui.X(PANEL_H));
            _restorePanel.Location = new Point(m + panelW + gap, pY);
            _restorePanel.Size = new Size((cw - m) - _restorePanel.Left, Ui.X(PANEL_H));

            // children of both action panels, in panel coordinates; hints
            // and notes span the full inner width with ellipsis, so no text
            // overflows its box in either language
            int pad = Ui.X(PAD);
            int inner = panelW - 2 * pad;
            int igap = Ui.X(8);
            _saveTag.Location = new Point(pad, Ui.X(12));
            _saveHint.Location = new Point(pad, Ui.X(30));
            _saveHint.Size = new Size(inner, Ui.X(14));
            _txtName.Location = new Point(pad, Ui.X(50));
            _txtName.Size = new Size(inner, Ui.X(26));
            _saveNote.Location = new Point(pad, Ui.X(90));
            _saveNote.Size = new Size(inner, Ui.X(14));

            // backup + lock always split the row in even halves: the widest
            // pt-br / en labels are measured short enough for either
            int half = (inner - igap) / 2;
            _btnBackup.Location = new Point(pad, Ui.X(BTN_Y));
            _btnBackup.Size = new Size(half, Ui.X(28));
            _btnLock.Location = new Point(pad + half + igap, Ui.X(BTN_Y));
            _btnLock.Size = new Size(inner - igap - half, Ui.X(28));

            _restoreTag.Location = new Point(pad, Ui.X(12));
            _restoreHint.Location = new Point(pad, Ui.X(30));
            _restoreHint.Size = new Size(inner, Ui.X(14));
            _cbSnap.Location = new Point(pad, Ui.X(50));
            _cbSnap.Size = new Size(inner, Ui.X(26));

            // destination dropdown + replace checkbox share one row; the TO
            // marker keeps the copy direction explicit and the checkbox is
            // measured from its real label so it never clips
            int chkW = ChkWidth(_chkReplace);
            int toW = Ui.X(36);
            _lblTo.Location = new Point(pad, Ui.X(90));
            _lblTo.Size = new Size(toW - Ui.X(4), Ui.X(14));
            _cbTarget.Location = new Point(pad + toW, Ui.X(84));
            _cbTarget.Size = new Size(inner - toW - igap - chkW, Ui.X(26));
            _chkReplace.Location = new Point(pad + inner - chkW, Ui.X(84));
            _chkReplace.Size = new Size(chkW, Ui.X(26));
            _btnRestore.Location = new Point(pad, Ui.X(BTN_Y));
            _btnRestore.Size = new Size(inner, Ui.X(28));

            int uY = pY + Ui.X(PANEL_H) + Ui.X(14);
            // folder row: buttons fit their labels, 50/50 when both spill
            int avail = cw - 2 * m;
            int wP = FitWidth(_btnPlayers), wK = FitWidth(_btnBackups);
            if (wP + wK + gap > avail) { wP = (avail - gap) / 2; wK = avail - gap - wP; }
            _btnPlayers.Location = new Point(m, uY);
            _btnPlayers.Size = new Size(wP, Ui.X(28));
            _btnBackups.Location = new Point(m + wP + gap, uY);
            _btnBackups.Size = new Size(wK, Ui.X(28));

            int lY = uY + Ui.X(28) + Ui.X(12);
            _footerLine.Location = new Point(m, lY);
            _footerLine.Width = avail;
            _status.Location = new Point(m, lY + Ui.X(12));
            _status.Size = new Size(avail, Ui.X(16));

            // an open snapshot dropdown follows its host across resizes
            _cbSnap.Reposition();
        }

        // checkbox hit width: real label + box + padding around it
        static int ChkWidth(CheckBox c)
        {
            int tw = TextRenderer.MeasureText(c.Text, c.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            return tw + Ui.X(13) + Ui.X(7) + Ui.X(6);
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
            if (idx < 0 || idx >= _games.Count || !_games[idx].Available) return;
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
            var names = new List<string>();
            if (_accounts.Count == 0)
                names.Add(T("list.empty"));
            else
                foreach (var item in _accounts)
                    names.Add(item.Id);
            _lb.SetItems(names);

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
            // destination dropdown mirrors the account list: platform marks
            // as icons, ids as text; keep the last choice when that account
            // still exists, otherwise follow the list selection
            string prevT = SelTargetId();
            _cbTarget.Items.Clear();
            var icons = new Bitmap[_accounts.Count];
            for (int i = 0; i < _accounts.Count; i++)
            {
                _cbTarget.Items.Add(_accounts[i].Id);
                icons[i] = PlatformMark(_accounts[i].Platform);
            }
            _cbTarget.Icons = icons;
            SelectTarget(string.IsNullOrEmpty(prevT) ? a?.Id : prevT);
            _cbTarget.Invalidate();
            UpdateLockButton();
            UpdateSaveHint();
            ApplyLayout();
            _lbPrevSel = _lb.SelectedIndex;
            _lb.Invalidate();
        }

        void UpdateLockButton()
        {
            var a = SelAcct();
            // LockedCount tracks the settings files only (all files except
            // .swp swap files), so any locked setting means "this account is locked"
            bool locked = a != null && a.LockedCount > 0;
            // icon matches the action the button will perform
            _btnLock.Text = T(locked ? "btn.lockoff" : "btn.lockon");
            _btnLock.Icon = Assets.Img(locked ? "ic-unlock.png" : "ic-lock.png");
        }

        string SelTargetId()
        {
            int ti = _cbTarget.SelectedIndex;
            return ti >= 0 && ti < _accounts.Count ? _accounts[ti].Id : null;
        }

        void SelectTarget(string id)
        {
            if (id == null) return;
            for (int i = 0; i < _cbTarget.Items.Count; i++)
                if (_cbTarget.Items[i] == id) { _cbTarget.SelectedIndex = i; return; }
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
                // land on the preset that was just saved so the apply panel
                // is ready for the next step
                int ni = -1;
                for (int k = 0; k < _snaps.Count; k++)
                    if (string.Equals(_snaps[k].Dir, dir, StringComparison.OrdinalIgnoreCase)) { ni = k; break; }
                _cbSnap.SelectedIndex = ni >= 0 ? ni : 0;
            }
            catch (Exception ex) { StatusErr(ex.Message); }
        }

        void DoRestore()
        {
            var g = SelGame();
            if (g == null || g.Resolved == null) { Status(T("st.nofolders")); return; }

            int i = _cbSnap.SelectedIndex;
            if (i < 0 || i >= _snaps.Count) { Status(T("st.nosnap")); return; }

            int ti = _cbTarget.SelectedIndex;
            if (ti < 0 || ti >= _accounts.Count)
            {
                Status(T("st.notarget"));
                return;
            }
            string id = _accounts[ti].Id;

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
            _lblTo.Text = T("lbl.to");
            _cbTarget.Placeholder = T("ph.none");
            _chkReplace.Text = T("chk.replace");
            _cbSnap.Placeholder = T("ph.none");
            _cbSnap.Invalidate();
            _cbTarget.Invalidate();

            FillList();
            PopulateSnapshots();

            Tip(_btnEn, "tip.en");
            Tip(_btnPt, "tip.pt");
            Tip(_btnMin, "tip.min");
            Tip(_btnMax, "tip.max");
            Tip(_btnClose, "tip.close");
            Tip(_btnRefresh, "tip.refresh");
            Tip(_btnBackup, "tip.backup");
            Tip(_btnRestore, "tip.restore");
            Tip(_btnLock, "tip.lock");
            Tip(_btnPlayers, "tip.players");
            Tip(_btnBackups, "tip.backups");
            Tip(_txtName, "tip.name");
            Tip(_cbSnap, "tip.snapshot");
            Tip(_cbTarget, "tip.target");
            Tip(_chkReplace, "tip.replace");
            Tip(_lb, "list.tip");
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].BrowseButton.Text = T("card.browse");
                Tip(_cards[i], _games[i].Available ? "tip.game" : "tip.game.off");
                Tip(_cards[i].BrowseButton, "tip.browse");
            }

            _lb.Invalidate();
            foreach (var c in _cards) c.Invalidate();

            Status(T("st.ready"));

            // labels changed (en/pt): re-fit every button and label to the
            // new text before any of it can clip
            ApplyLayout();
        }

        // -------------------------------------------------------- tooltips
        // winforms' native tooltip forces segoe ui; the tip is a borderless
        // popup painted in hitmarker instead. labels resolve through l10n
        // keys at show time, so a language switch never leaves a stale tip

        void Tip(Control c, string key)
        {
            if (c == null) return;
            _tips[c] = key;
            if (_tipHooked.Add(c))
            {
                c.MouseEnter += TipEnter;
                c.MouseLeave += TipLeave;
            }
        }

        void TipEnter(object s, EventArgs e)
        {
            Control c = s as Control;
            HideTip();
            if (c == null) return;
            _tipHot = c;
            _tipTimer.Interval = 250;
            _tipTimer.Stop();
            _tipTimer.Start();
        }

        void TipLeave(object s, EventArgs e)
        {
            // a leave can arrive after a newer control's enter queued its own
            // tick; only the current hot control may cancel the pending show
            if (!ReferenceEquals(_tipHot, s)) return;
            _tipHot = null;
            _tipTimer.Stop();
            HideTip();
        }

        void TipTick(object s, EventArgs e)
        {
            _tipTimer.Stop();
            if (_tipHot == null) return;
            if (_tipPop == null || !_tipPop.Visible)
            {
                ShowTip();
                _tipShown = DateTime.Now;
                _tipTimer.Interval = 10000; // auto-hide once the 10s expire
                _tipTimer.Start();
            }
            else if ((DateTime.Now - _tipShown).TotalSeconds >= 10)
                HideTip();
        }

        void ShowTip()
        {
            string key;
            if (_tipHot == null || IsDisposed || !_tips.TryGetValue(_tipHot, out key)) return;
            string text = T(key);
            if (string.IsNullOrEmpty(text)) return;
            if (_tipPop == null) _tipPop = new TipPopup();
            _tipPop.SetText(text);
            Rectangle host = _tipHot.RectangleToScreen(new Rectangle(0, 0, _tipHot.Width, _tipHot.Height));
            _tipPop.ShowAt(_tipPop.PreferredAt(host));
        }

        void HideTip()
        {
            if (_tipPop != null && !_tipPop.IsDisposed && _tipPop.Visible) _tipPop.Hide();
            _tipHot = null;
            _tipShown = DateTime.MinValue;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // esc/space skip the intro, esc always clears a showing tip
            if (_intro != null && _intro.Visible &&
                (keyData == Keys.Escape || keyData == Keys.Space))
            {
                _intro.Skip();
                return true;
            }
            if (keyData == Keys.Escape) HideTip();
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // -------------------------------------------------------------- intro
        // the kit gif plays once over the whole client area at startup,
        // memory only, at the file's own frame delays

        void StartIntro()
        {
            if (DesignMode) return;
            _intro = new IntroOverlay();
            if (!_intro.LoadGif())
            {
                _intro.Dispose();
                _intro = null;
                return;
            }
            Controls.Add(_intro);
            _intro.BringToFront();
            _intro.Done += (s, e) =>
            {
                if (_intro != null)
                {
                    Controls.Remove(_intro);
                    _intro.Dispose();
                    _intro = null;
                }
            };
            _intro.Play();
        }

        // -------------------------------------------------------------- list draw

        void Lb_PaintRow(Graphics g, int index, Rectangle bounds, bool sel)
        {
            using (var back = new SolidBrush(sel ? Ui.SelRow : Ui.Panel))
                g.FillRectangle(back, bounds);
            if (sel)
                using (var b = new SolidBrush(Ui.Accent))
                    g.FillRectangle(b, bounds.X, bounds.Y, Ui.X(2), bounds.Height);

            if (index >= _accounts.Count) // placeholder row
            {
                TextRenderer.DrawText(g, _lb[index], Ui.Small,
                    new Point(bounds.X + Ui.X(12), bounds.Y + Ui.X(6)), Ui.Faint,
                    TextFormatFlags.NoPadding);
                return;
            }

            var a = _accounts[index];
            int bx = bounds.X, by = bounds.Y + Ui.X(6);
            int bw = bounds.Width;
            int tx = bx + Ui.X(12);
            // columns stay proportional to the live row width, floored so
            // they never pile up wide or clip narrow; the id gets its own
            // ellipsised cell up to the platform column
            int colP = Math.Max(tx + Ui.X(64), bx + (int)(bw * 0.30f));
            int colF = Math.Max(colP + Ui.X(72), bx + (int)(bw * 0.52f));
            int colL = Math.Max(colF + Ui.X(56), bx + (int)(bw * 0.74f));
            var mark = PlatformMark(a.Platform);
            if (mark != null)
            {
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(mark, new Rectangle(bx + Ui.X(10), by, Ui.X(14), Ui.X(14)));
                g.InterpolationMode = old;
                tx = bx + Ui.X(32);
            }
            TextRenderer.DrawText(g, a.Id, Ui.Num(9.5f),
                new Rectangle(tx, by, colP - tx - Ui.X(8), Ui.X(16)), Ui.Text,
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            Col(g, a.Platform,
                a.Platform == "unknown" || a.Platform.EndsWith("?") ? Ui.Faint : Ui.Accent,
                colP, by);
            Col(g, string.Format(T("list.files"), a.FileCount), sel ? Ui.Text : Ui.Dim, colF, by);
            if (a.LockedCount > 0)
                Col(g, string.Format(T("list.locked"), a.LockedCount), sel ? Ui.Text : Ui.Faint, colL, by);
        }

        static void Col(Graphics g, string s, Color c, int x, int y, Font f = null) =>
            TextRenderer.DrawText(g, s, f ?? Ui.Body, new Point(x, y), c, TextFormatFlags.NoPadding);
    }

    // one-shot intro: the embedded kit gif over the whole client area,
    // memory only, playing once at the file's own frame delays
    class IntroOverlay : Panel
    {
        Image _gif;
        Stream _keepAlive; // image.fromstream keeps the stream alive for life
        int[] _ms;
        int _frame;
        int _acc;
        System.Windows.Forms.Timer _t;
        bool _done;

        public event EventHandler Done;

        public IntroOverlay()
        {
            Dock = DockStyle.Fill;
            BackColor = Ui.Bg;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            MouseClick += (s, e) => Skip();
        }

        public bool LoadGif()
        {
            Stream s = typeof(MainForm).Assembly.GetManifestResourceStream("img/intro.gif");
            if (s == null) return false;
            try
            {
                _gif = Image.FromStream(s);
                _keepAlive = s;
                _ms = FrameDelays(_gif);
                return true;
            }
            catch
            {
                if (_gif != null) _gif.Dispose();
                s.Dispose();
                return false;
            }
        }

        // gif delay table (property 0x5100, centiseconds); zero entries
        // mean 100ms per the spec, and a single frame without a table
        // holds the whole file duration (~2.1s)
        static int[] FrameDelays(Image img)
        {
            try
            {
                if (img.FrameDimensionsList.Length > 0)
                {
                    int n = img.GetFrameCount(new FrameDimension(img.FrameDimensionsList[0]));
                    byte[] tab = null;
                    foreach (var p in img.PropertyItems)
                        if (p.Id == 0x5100) { tab = p.Value; break; }
                    var list = new List<int>(n);
                    for (int f = 0; f < n; f++)
                    {
                        int cs = 0;
                        if (tab != null && f * 2 + 1 < tab.Length)
                            cs = tab[f * 2] | (tab[f * 2 + 1] << 8);
                        list.Add(tab == null && n == 1 ? 2100 : (cs == 0 ? 100 : cs * 10));
                    }
                    return list.ToArray();
                }
            }
            catch { }
            return new[] { 2100 };
        }

        public void Play()
        {
            _t = new System.Windows.Forms.Timer { Interval = 30 };
            _t.Tick += Tick;
            _t.Start();
            Invalidate();
        }

        void Tick(object s, EventArgs e)
        {
            _acc += 30;
            while (_frame < _ms.Length && _acc >= _ms[_frame])
            {
                _acc -= _ms[_frame];
                _frame++;
            }
            if (_frame >= _ms.Length) { Skip(); return; }
            Invalidate();
        }

        public void Skip()
        {
            if (_done) return;
            _done = true;
            if (_t != null) { _t.Stop(); _t.Dispose(); _t = null; }
            if (Done != null) Done(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Ui.Bg);
            if (_gif != null) Img.Fit(e.Graphics, _gif, _frame, ClientRectangle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_t != null) { _t.Stop(); _t.Dispose(); }
                if (_gif != null) _gif.Dispose();
                if (_keepAlive != null) _keepAlive.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // borderless hitmarker tooltip bubble; toolwindow + noactivate keeps it
    // out of alt-tab and off the activation chain
    class TipPopup : Form
    {
        string _text = "";

        public TipPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Ui.PanelHi;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x0080 | 0x08000000; // WS_EX_TOOLWINDOW | NOACTIVATE
                return cp;
            }
        }

        public void SetText(string text)
        {
            _text = text ?? "";
            // wrap long hints instead of clipping them mid-word
            int maxW = Ui.X(320);
            Size ts = TextRenderer.MeasureText(_text, Ui.Small,
                new Size(maxW, Ui.X(600)), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            ClientSize = new Size(Math.Min(ts.Width, maxW) + Ui.X(20), ts.Height + Ui.X(12));
        }

        // under the host, flipping above when the screen runs out; clamped
        // into the owning work area either way
        public Point PreferredAt(Rectangle host)
        {
            Point p = new Point(host.X, host.Bottom + Ui.X(6));
            Rectangle wa = Screen.FromRectangle(host).WorkingArea;
            if (p.Y + Height > wa.Bottom) p.Y = host.Top - Height - Ui.X(6);
            if (p.Y < wa.Top) p.Y = wa.Top;
            if (p.X < wa.Left) p.X = wa.Left;
            if (p.X + Width > wa.Right) p.X = wa.Right - Width;
            return p;
        }

        public void ShowAt(Point p)
        {
            Location = p;
            // managed Visible can outlive the native window state after an
            // aborted first show; reconcile before re-showing
            if (Visible && IsHandleCreated && !IsWindowVisible(Handle)) Hide();
            if (!Visible) Show();
            BringToFront();
        }

        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(Ui.PanelHi)) g.FillRectangle(b, ClientRectangle);
            using (var p = new Pen(Ui.Line))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(g, _text, Ui.Small,
                new Rectangle(Ui.X(10), Ui.X(6), ClientSize.Width - Ui.X(20), ClientSize.Height - Ui.X(12)),
                Ui.Text, TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
