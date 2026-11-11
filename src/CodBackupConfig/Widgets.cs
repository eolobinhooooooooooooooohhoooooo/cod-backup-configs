using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CodBackupConfig
{
    // flat button, 1px border, hover shifts the border to the accent color
    class LineButton : Button
    {
        bool _hot;
        protected bool Hot => _hot;
        public bool Highlight { get; set; }
        public Bitmap Icon { get; set; }

        public LineButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Ui.Text;
            BackColor = Ui.Panel;
            Cursor = Cursors.Hand;
            MouseEnter += (s, e) => { _hot = true; Invalidate(); };
            MouseLeave += (s, e) => { _hot = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = !Enabled ? Ui.Bg : Highlight ? Ui.AccentDim : _hot ? Ui.PanelHi : Ui.Panel;
            using (var b = new SolidBrush(back)) g.FillRectangle(b, ClientRectangle);
            using (var p = new Pen(Highlight || _hot ? Ui.Accent : Ui.Line))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            Color fore = !Enabled ? Ui.Faint : _hot && !Highlight ? Ui.Accent : Ui.Text;
            if (Icon != null)
            {
                // 14x14 icon on the left, label centered in the rest
                int iw = Ui.X(14), ix = Ui.X(10);
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Icon, new Rectangle(ix, (Height - iw) / 2, iw, iw));
                g.InterpolationMode = old;
                TextRenderer.DrawText(g, Text, Font,
                    new Rectangle(ix + iw + Ui.X(6), 0, Width - ix - iw - Ui.X(16), Height), fore,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                return;
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }
    }

    // line button with a flag image on the left (language switcher)
    class FlagButton : LineButton
    {
        public Bitmap Flag { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = Highlight ? Ui.AccentDim : Hot ? Ui.PanelHi : Ui.Panel;
            using (var b = new SolidBrush(back)) g.FillRectangle(b, ClientRectangle);
            using (var p = new Pen(Highlight || Hot ? Ui.Accent : Ui.Line))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            int fw = Ui.X(22), fh = Ui.X(15), fx = Ui.X(9);
            if (Flag != null) Img.Fit(g, Flag, new Rectangle(fx, (Height - fh) / 2, fw, fh));
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(fx + fw + Ui.X(4), 0, Width - fx - fw - Ui.X(9), Height),
                Highlight || Hot ? Ui.Accent : Ui.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    // frameless caption controls: minimize dash / maximize square / close
    // cross with windows-style hover (close turns red)
    class WindowButton : Button
    {
        public enum Kind { Minimize, Maximize, Close }

        readonly Kind _kind;
        bool _hot;

        public WindowButton(Kind kind)
        {
            _kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Ui.Header;
            ForeColor = Ui.Dim;
            Cursor = Cursors.Hand;
            TabStop = false;
            MouseEnter += (s, e) => { _hot = true; Invalidate(); };
            MouseLeave += (s, e) => { _hot = false; Invalidate(); };
            Click += (s, e) =>
            {
                var f = FindForm();
                if (f == null) return;
                if (_kind == Kind.Minimize)
                    f.WindowState = FormWindowState.Minimized;
                else if (_kind == Kind.Maximize)
                    f.WindowState = f.WindowState == FormWindowState.Maximized
                        ? FormWindowState.Normal
                        : FormWindowState.Maximized;
                else
                    f.Close();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            bool red = _kind == Kind.Close && _hot;
            using (var b = new SolidBrush(red ? Ui.Error : (_hot ? Ui.PanelHi : Ui.Header)))
                g.FillRectangle(b, ClientRectangle);
            Color fore = red ? Color.White : (_hot ? Ui.Text : Ui.Dim);
            int cx = Width / 2, cy = Height / 2;
            using (var p = new Pen(fore))
            {
                if (_kind == Kind.Minimize)
                    g.DrawLine(p, cx - Ui.X(5), cy, cx + Ui.X(5), cy);
                else if (_kind == Kind.Maximize)
                {
                    if (FindForm()?.WindowState == FormWindowState.Maximized)
                    {
                        // restore glyph: two offset squares
                        g.DrawRectangle(p, cx - Ui.X(1), cy - Ui.X(5), Ui.X(7), Ui.X(7));
                        g.DrawRectangle(p, cx - Ui.X(4), cy - Ui.X(2), Ui.X(7), Ui.X(7));
                    }
                    else
                        g.DrawRectangle(p, cx - Ui.X(4), cy - Ui.X(4), Ui.X(8), Ui.X(8));
                }
                else
                {
                    g.DrawLine(p, cx - Ui.X(5), cy - Ui.X(5), cx + Ui.X(5), cy + Ui.X(5));
                    g.DrawLine(p, cx - Ui.X(5), cy + Ui.X(5), cx + Ui.X(5), cy - Ui.X(5));
                }
            }
        }
    }

    // 1px bordered container for the account list
    class BorderPanel : Panel
    {
        public BorderPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Padding = new Padding(1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            using var p = new Pen(Ui.Line);
            e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }

    class SectionLabel : Label
    {
        public SectionLabel()
        {
            AutoSize = true;
            Font = Ui.Head(10f);
            ForeColor = Ui.Faint;
            BackColor = Color.Transparent;
        }
    }

    // clickable game card: cover art full-bleed with a dark gradient,
    // text laid over it (the rehab.ac product-card pattern)
    class GameCard : Control
    {
        GameInfo _g;
        Bitmap _cover;
        Bitmap _logo;
        bool _hot;
        bool _found;
        int _accts;
        Button _browse;

        public event Action Clicked;
        public event Action<GameInfo> BrowseClick;
        public Button BrowseButton => _browse;
        public bool Selected { get; private set; }

        public GameCard(GameInfo g)
        {
            _g = g;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Panel;
            Cursor = Cursors.Hand;
            _cover = Assets.Img(g.Key + ".jpg");
            _logo = Assets.Img("logo-" + g.Key + ".png");

            _browse = new Button
            {
                Visible = false,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Ui.PanelHi,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
            };
            _browse.FlatAppearance.BorderColor = Ui.Line;
            _browse.FlatAppearance.BorderSize = 1;
            _browse.Click += (s, e) => BrowseClick?.Invoke(_g);
            Controls.Add(_browse);

            MouseEnter += (s, e) => { _hot = true; Invalidate(); };
            MouseLeave += (s, e) => { _hot = false; Invalidate(); };
            MouseClick += (s, e) => Clicked?.Invoke();
        }

        public void SetSelected(bool v)
        {
            if (Selected == v) return;
            Selected = v;
            Invalidate();
        }

        public void SetState(bool found, int accounts)
        {
            _found = found;
            _accts = accounts;
            _browse.Visible = !found;
            LayoutBrowse();
            Invalidate();
        }

        void LayoutBrowse() =>
            _browse.Bounds = new Rectangle(Width - Ui.X(140), Height - Ui.X(28), Ui.X(128), Ui.X(20));

        protected override void OnLayout(LayoutEventArgs e) => LayoutBrowse();

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = ClientRectangle;
            using (var b = new SolidBrush(_hot ? Ui.PanelHi : Ui.Panel))
                g.FillRectangle(b, r);

            if (_found && _cover != null)
            {
                CoverCrop(g, _cover, r);
                using (var lg = new LinearGradientBrush(r, Ui.Bg, Ui.Bg, LinearGradientMode.Horizontal))
                {
                    // keep the text zone dark, a thin slice of art mid-card,
                    // heavy veil on the tail so the cover logo stays muted
                    var blend = new ColorBlend
                    {
                        Colors = new[]
                        {
                            Color.FromArgb(246, Ui.Bg),
                            Color.FromArgb(241, Ui.Bg),
                            Color.FromArgb(253, Ui.Bg),
                        },
                        Positions = new[] { 0f, 0.45f, 1f },
                    };
                    lg.InterpolationColors = blend;
                    g.FillRectangle(lg, r);
                }
            }

            using (var p = new Pen(Selected ? Ui.Accent : _hot ? Ui.Dim : Ui.Line))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            // the official lockup is the title; plain name is the fallback
            if (_found && _logo != null)
            {
                // ~55% of the card width, hugging the top-left; capped to a
                // 44px strip so the lockup never reaches the bottom lanes
                int lw = Math.Max(Ui.X(24), (int)(Width * 0.55f) - Ui.X(12));
                // the strip grows with taller cards so the lockup stays
                // proportional instead of pinned at the base size
                int lh = Math.Min(Math.Max(Ui.X(44), (int)(Height * 0.40f)), Height - Ui.X(60));
                Img.Fit(g, _logo, new Rectangle(Ui.X(12), Ui.X(8), lw, lh));
            }
            else
                TextRenderer.DrawText(g, _g.Name, Ui.CardName,
                    new Rectangle(Ui.X(12), Ui.X(14), Width - Ui.X(24), Ui.X(18)), Ui.Text, TextFormatFlags.NoPadding);

            if (_found)
            {
                // path and account counter sit in separate lanes 18px apart
                // so the two lines can never touch, even in pt-br
                using var sf = new StringFormat(StringFormatFlags.NoWrap);
                sf.Trimming = StringTrimming.EllipsisCharacter;
                using var b = new SolidBrush(Ui.Dim);
                g.DrawString(_g.Resolved, Ui.Small, b,
                    new RectangleF(Ui.X(12), Height - Ui.X(38), Width - Ui.X(24), Ui.X(14)), sf);

                TextRenderer.DrawText(g,
                    string.Format(L10n.T(_accts == 1 ? "card.account" : "card.accounts"), _accts), Ui.Small,
                    new Rectangle(Ui.X(12), Height - Ui.X(20), Width - Ui.X(24), Ui.X(14)), Ui.Accent,
                    TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, L10n.T("card.notfound"), Ui.Small,
                    new Rectangle(Ui.X(12), Ui.X(40), Width - Ui.X(24), Ui.X(14)), Ui.Faint,
                    TextFormatFlags.NoPadding);
            }
        }

        // fill the rect, center-crop (css object-fit: cover)
        static void CoverCrop(Graphics g, Bitmap img, Rectangle r)
        {
            var old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            float ar = img.Width / (float)img.Height;
            float rr = r.Width / (float)r.Height;
            if (ar > rr)
            {
                int w = (int)(r.Height * ar);
                g.DrawImage(img, r.X + (r.Width - w) / 2, r.Y, w, r.Height);
            }
            else
            {
                int h = (int)(r.Width / ar);
                g.DrawImage(img, r.X, r.Y + (r.Height - h) / 2, r.Width, h);
            }
            g.InterpolationMode = old;
        }
    }

    // flat dropdown without the native combo chrome
    class ChoiceBox : Control
    {
        public List<string> Items { get; } = new();
        public Bitmap[] Icons { get; set; }  // per-item left mark, may be null
        public string Placeholder { get; set; } = "";
        public event Action<int> Changed;

        int _sel = -1;
        Panel _shell;   // non-null while the dropdown is open
        MouseEventHandler _outside;
        List<Control> _hooked = new();
        DarkListBox _lb;
        EventHandler _deact;

        public int SelectedIndex
        {
            get => _sel;
            set { _sel = value; Invalidate(); }
        }

        public ChoiceBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Panel;
            Cursor = Cursors.Hand;
            _deact = (s, e) => Close();
            Disposed += (s, e) => Close();
        }

        string Current => _sel >= 0 && _sel < Items.Count ? Items[_sel] : null;

        bool HasIcon(int i) => i >= 0 && i < (Icons?.Length ?? 0) && Icons[i] != null;

        void DrawIcon(Graphics g, int i, Rectangle box)
        {
            var old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(Icons[i], box);
            g.InterpolationMode = old;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Toggle();
        }

        void Toggle() { if (_shell == null) Open(); else Close(); }

        public void Dismiss() => Close();

        // a resize while the dropdown is open moves the host; follow the
        // fresh host geometry (width and below/above flip included)
        public void Reposition()
        {
            if (_shell == null) return;
            var host = FindForm();
            if (host == null) return;
            _shell.Width = Width;
            Point ho = host.PointToClient(PointToScreen(new Point(0, 0)));
            int h = _shell.Height;
            bool below = ho.Y + Height + Ui.X(2) + h <= host.ClientSize.Height;
            _shell.Location = below
                ? new Point(ho.X, ho.Y + Height + Ui.X(2))
                : new Point(ho.X, ho.Y - Ui.X(2) - h);
        }

        void Open()
        {
            if (Items.Count == 0 || _shell != null) return;
            var host = FindForm();
            if (host == null) return;

            _shell = new Panel { Padding = new Padding(1), BackColor = Ui.Bg };
            _shell.Paint += (s, e) =>
            {
                e.Graphics.Clear(Ui.Bg);
                using var p = new Pen(Ui.Line);
                e.Graphics.DrawRectangle(p, 0, 0, _shell.Width - 1, _shell.Height - 1);
            };
            _lb = new DarkListBox
            {
                Dock = DockStyle.Fill,
                RowHeight = Ui.X(26),
                RowPainter = (g, i, r, sel) =>
                {
                    using var b = new SolidBrush(sel ? Ui.AccentDim : Ui.Panel);
                    g.FillRectangle(b, r);
                    int tx = r.X + Ui.X(12);
                    if (HasIcon(i))
                    {
                        DrawIcon(g, i, new Rectangle(r.X + Ui.X(6), r.Y + Ui.X(6), Ui.X(14), Ui.X(14)));
                        tx += Ui.X(20);
                    }
                    TextRenderer.DrawText(g, _lb[i], Ui.Body,
                        new Point(tx, r.Y + Ui.X(6)),
                        sel ? Ui.Text : Ui.Dim, TextFormatFlags.NoPadding);
                },
            };
            _lb.SetItems(Items);
            if (_sel >= 0) _lb.SelectedIndex = _sel;
            _lb.ItemClicked += i =>
            {
                _sel = i;
                Close();
                Invalidate();
                Changed?.Invoke(_sel);
            };
            _shell.Controls.Add(_lb);
            int h = Math.Min(Items.Count, 6) * Ui.X(26) + Ui.X(2);
            _shell.Size = new Size(Width, h);

            // dock below us, or above us if the window would clip it
            Point ho = host.PointToClient(PointToScreen(new Point(0, 0)));
            bool below = ho.Y + Height + Ui.X(2) + h <= host.ClientSize.Height;
            _shell.Location = below
                ? new Point(ho.X, ho.Y + Height + Ui.X(2))
                : new Point(ho.X, ho.Y - Ui.X(2) - h);
            host.Controls.Add(_shell);
            _shell.BringToFront();
            // WinForms has no mouse-event bubbling, and a click-catcher panel
            // would hide the UI behind it, so hook MouseDown on every control
            // instead: any press outside the box and the open shell closes it
            _outside = (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (ReferenceEquals(s, this) || ReferenceEquals(s, _shell) || ReferenceEquals(s, _lb)) return;
                Close();
            };
            HookControls(host);
            host.Deactivate += _deact;
        }

        void HookControls(Control root)
        {
            root.MouseDown += _outside;
            _hooked.Add(root);
            foreach (Control c in root.Controls)
                HookControls(c);
        }

        void Close()
        {
            if (_shell == null) return;
            var host = FindForm();
            if (host != null) host.Deactivate -= _deact;
            foreach (var c in _hooked)
                if (!c.IsDisposed) c.MouseDown -= _outside;
            _hooked.Clear();
            Rectangle was = _shell.Bounds;
            _shell.Controls.Remove(_lb);
            host?.Controls.Remove(_shell);
            _shell.Dispose();
            _shell = null;
            _lb = null;
            // repaint whatever the open dropdown was covering
            host?.Invalidate(was);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(Ui.Panel)) g.FillRectangle(b, ClientRectangle);
            string t = Current;
            Color c = t == null ? Ui.Faint : Ui.Text;
            using (var p = new Pen(_shell != null ? Ui.Accent : Ui.Line))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            int tx = Ui.X(10);
            if (t != null && HasIcon(_sel))
            {
                DrawIcon(g, _sel, new Rectangle(Ui.X(8), (Height - Ui.X(14)) / 2, Ui.X(14), Ui.X(14)));
                tx = Ui.X(28);
            }
            TextRenderer.DrawText(g, t ?? Placeholder, Font,
                new Rectangle(tx, 0, Width - tx - Ui.X(32), Height), c,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            // chevron
            using (var pen = new Pen(c))
            {
                int cx = Width - Ui.X(17), cy = Height / 2;
                g.DrawLines(pen, new[]
                {
                    new Point(cx - Ui.X(3), cy - Ui.X(2)), new Point(cx, cy + Ui.X(2)), new Point(cx + Ui.X(3), cy - Ui.X(2)),
                });
            }
        }
    }

    // flat checkbox matching the palette
    class TickBox : CheckBox
    {
        public TickBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            BackColor = Ui.Bg;
            ForeColor = Ui.Dim;
            AutoEllipsis = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);
            int s = Ui.X(13), x = Ui.X(1), y = (Height - s) / 2;
            using (var b = new SolidBrush(Checked ? Ui.AccentDim : Ui.Panel))
                g.FillRectangle(b, x, y, s, s);
            using (var p = new Pen(Checked ? Ui.Accent : Ui.Line))
                g.DrawRectangle(p, x, y, s, s);
            if (Checked)
                using (var p = new Pen(Ui.Text))
                    g.DrawLines(p, new[]
                    {
                        new Point(x + Ui.X(3), y + Ui.X(7)), new Point(x + Ui.X(5), y + Ui.X(9)), new Point(x + Ui.X(9), y + Ui.X(4)),
                    });
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(x + s + Ui.X(7), 0, Width - x - s - Ui.X(8), Height),
                Checked ? Ui.Text : Ui.Dim,
                TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPadding);
        }
    }

    // owner-drawn list with no win32 chrome: the consumer paints every row
    // through RowPainter, and a slim 6px thumb-and-track bar flush to the
    // right edge only exists while the rows overflow. wheel, keys and
    // dragging the bar (or pressing the track) scroll it
    class DarkListBox : Control
    {
        public event EventHandler SelectedIndexChanged;
        public event Action<int> ItemClicked;
        public Action<Graphics, int, Rectangle, bool> RowPainter;

        string[] _items = Array.Empty<string>();
        int _rh = 26;
        int _top;
        int _sel = -1;
        bool _grab, _hot;
        int _grabDy;       // cursor offset from the thumb center while dragging
        Rectangle _thumb;

        public DarkListBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Panel;
            TabStop = true;
            LostFocus += (s, e) => EndDrag();
        }

        public int Count => _items.Length;
        public string this[int i] => i >= 0 && i < _items.Length ? _items[i] : null;

        public int RowHeight
        {
            get => _rh;
            set { _rh = Math.Max(Ui.X(8), value); ClampTop(); Invalidate(); }
        }

        public int SelectedIndex
        {
            get => _sel;
            set
            {
                int v = Math.Clamp(value, -1, Math.Max(-1, _items.Length - 1));
                if (v == _sel) return;
                _sel = v;
                if (v >= 0) EnsureVisible(v);
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // replaces the whole item list without firing selection events; the
        // caller reselects through SelectedIndex right after
        public void SetItems(IEnumerable<string> items)
        {
            var l = new List<string>();
            if (items != null) foreach (string s in items) l.Add(s);
            _items = l.ToArray();
            _top = 0;
            if (_sel >= _items.Length) _sel = _items.Length > 0 ? _items.Length - 1 : -1;
            Invalidate();
        }

        int VisibleRows => Math.Max(1, Height / _rh);
        bool Overflow => _items.Length > VisibleRows;
        int MaxOff => Math.Max(0, _items.Length - VisibleRows);

        void ClampTop() { _top = Math.Clamp(_top, 0, MaxOff); }

        void EnsureVisible(int i)
        {
            if (!Overflow) return;
            if (i < _top) _top = i;
            else if (i >= _top + VisibleRows) _top = i - VisibleRows + 1;
        }

        Rectangle BarRect()
        {
            int bw = Ui.X(6);
            return new Rectangle(Width - Ui.X(2) - bw, 0, bw, Height);
        }

        Rectangle ThumbRect()
        {
            var bar = BarRect();
            int vis = VisibleRows;
            int th = Count > 0 ? (int)(bar.Height * vis / (double)Count) : bar.Height;
            th = Math.Clamp(th, _rh, bar.Height);
            int span = Math.Max(1, bar.Height - th);
            int y = MaxOff == 0 ? 0 : (int)((bar.Height - th) * _top / (double)MaxOff);
            return new Rectangle(bar.X, Math.Clamp(y, 0, span), bar.Width, th);
        }

        // move the view so the thumb is centered on y
        void MoveThumbTo(int y)
        {
            if (MaxOff == 0) return;
            var bar = BarRect();
            var t = ThumbRect();
            int yy = Math.Clamp(y - t.Height / 2, 0, bar.Height - t.Height);
            int span = Math.Max(1, bar.Height - t.Height);
            _top = Math.Clamp((int)Math.Round(yy * MaxOff / (double)span), 0, MaxOff);
        }

        void SelectRowAt(Point p)
        {
            int i = _top + p.Y / _rh;
            if (p.Y < 0 || i < 0 || i >= _items.Length) return;
            if (_sel != i) SelectedIndex = i;
            ItemClicked?.Invoke(i);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ClampTop();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);
            // rows stop short of the bar so no text ever runs under the thumb
            int rx = Overflow ? Width - Ui.X(2) - Ui.X(6) : Width;
            g.SetClip(new Rectangle(0, 0, rx, Height));
            for (int v = 0; v < VisibleRows; v++)
            {
                int i = _top + v;
                if (i < 0 || i >= _items.Length) break;
                RowPainter?.Invoke(g, i, new Rectangle(0, v * _rh, rx, _rh), i == _sel);
            }
            g.ResetClip();
            if (!Overflow) { _thumb = Rectangle.Empty; return; }
            var bar = BarRect();
            using (var b = new SolidBrush(Ui.ScrollTrack)) g.FillRectangle(b, bar);
            _thumb = ThumbRect();
            using (var b = new SolidBrush((_hot || _grab) ? Ui.Accent : Ui.ScrollThumb))
                g.FillRectangle(b, _thumb);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left) return;
            if (Overflow && e.X >= BarRect().Left)
            {
                if (_thumb.Contains(e.Location))
                    _grabDy = e.Y - _thumb.Y - _thumb.Height / 2;
                else
                {
                    // pressing the track repositions the thumb there and
                    // carries on as if the cursor held its center
                    MoveThumbTo(e.Y);
                    _grabDy = 0;
                }
                _grab = true;
                Cursor = Cursors.SizeNS;
                Invalidate();
                return;
            }
            SelectRowAt(e.Location);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_grab)
            {
                MoveThumbTo(e.Y - _grabDy);
                Invalidate();
                return;
            }
            bool hot = Overflow && e.X >= BarRect().Left;
            if (hot != _hot) { _hot = hot; Cursor = hot ? Cursors.SizeNS : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            EndDrag();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_grab && _hot) { _hot = false; Cursor = Cursors.Default; Invalidate(); }
        }

        void EndDrag()
        {
            if (!_grab) return;
            _grab = false;
            _hot = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Overflow) return;
            _top = Math.Clamp(_top - e.Delta / 120 * 3, 0, MaxOff);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData) =>
            base.IsInputKey(keyData) ||
            keyData == Keys.Up || keyData == Keys.Down ||
            keyData == Keys.PageUp || keyData == Keys.PageDown;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_items.Length == 0) return;
            int page = VisibleRows - 1;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    SelectedIndex = _sel <= 0 ? _items.Length - 1 : _sel - 1;
                    e.Handled = true;
                    break;
                case Keys.Down:
                    SelectedIndex = _sel < 0 ? 0 : _sel + 1 >= _items.Length ? 0 : _sel + 1;
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    SelectedIndex = Math.Max(0, (_sel < 0 ? page : _sel) - page);
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    SelectedIndex = _sel < 0 ? Math.Min(page, _items.Length - 1) : Math.Min(_items.Length - 1, _sel + page);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case Keys.End:
                    SelectedIndex = _items.Length - 1;
                    e.Handled = true;
                    break;
            }
        }
    }
}
