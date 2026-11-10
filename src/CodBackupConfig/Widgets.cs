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

    // frameless caption controls: minimize dash / close cross with
    // windows-style hover (close turns red)
    class WindowButton : Button
    {
        public enum Kind { Minimize, Close }

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
                if (_kind == Kind.Minimize) f.WindowState = FormWindowState.Minimized;
                else f.Close();
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
                    // keep the text zone dark, let a slice of art show mid-card,
                    // darken the tail so the cover logo stops competing
                    var blend = new ColorBlend
                    {
                        Colors = new[]
                        {
                            Color.FromArgb(240, Ui.Bg),
                            Color.FromArgb(225, Ui.Bg),
                            Color.FromArgb(248, Ui.Bg),
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
                int lh = Math.Min(Ui.X(44), Height - Ui.X(60));
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
        ListBox _lb;
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
            _lb = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Ui.Panel,
                ForeColor = Ui.Text,
                Font = Font,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = Ui.X(26),
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
            };
            foreach (string i in Items) _lb.Items.Add(i);
            if (_sel >= 0 && _sel < _lb.Items.Count) _lb.SelectedIndex = _sel;
            _lb.DrawItem += (s, e) =>
            {
                bool sel = e.Index == _lb.SelectedIndex;
                using var b = new SolidBrush(sel ? Ui.AccentDim : Ui.Panel);
                e.Graphics.FillRectangle(b, e.Bounds);
                int tx = e.Bounds.X + Ui.X(12);
                if (HasIcon(e.Index))
                {
                    DrawIcon(e.Graphics, e.Index, new Rectangle(e.Bounds.X + Ui.X(6), e.Bounds.Y + Ui.X(6), Ui.X(14), Ui.X(14)));
                    tx += Ui.X(20);
                }
                TextRenderer.DrawText(e.Graphics, _lb.Items[e.Index].ToString(), Ui.Body,
                    new Point(tx, e.Bounds.Y + Ui.X(6)),
                    sel ? Ui.Text : Ui.Dim, TextFormatFlags.NoPadding);
            };
            _lb.Click += (s, e) =>
            {
                _sel = _lb.SelectedIndex;
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
}
