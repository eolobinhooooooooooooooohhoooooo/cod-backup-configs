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
                int iw = 14, ix = 10;
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Icon, new Rectangle(ix, (Height - iw) / 2, iw, iw));
                g.InterpolationMode = old;
                TextRenderer.DrawText(g, Text, Font,
                    new Rectangle(ix + iw + 6, 0, Width - ix - iw - 16, Height), fore,
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

            int fw = 22, fh = 15;
            if (Flag != null) Img.Fit(g, Flag, new Rectangle(9, (Height - fh) / 2, fw, fh));
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(9 + fw + 4, 0, Width - 9 - fw - 9, Height),
                Highlight || Hot ? Ui.Accent : Ui.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
            _browse.Bounds = new Rectangle(Width - 140, Height - 28, 128, 20);

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
                            Color.FromArgb(248, Ui.Bg),
                            Color.FromArgb(195, Ui.Bg),
                            Color.FromArgb(238, Ui.Bg),
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
                Img.Fit(g, _logo, new Rectangle(12, 8, Width - 24, Height - 50));
            else
                TextRenderer.DrawText(g, _g.Name, Ui.CardName,
                    new Rectangle(12, 14, Width - 24, 18), Ui.Text, TextFormatFlags.NoPadding);

            if (_found)
            {
                using var sf = new StringFormat(StringFormatFlags.NoWrap);
                sf.Trimming = StringTrimming.EllipsisCharacter;
                using var b = new SolidBrush(Ui.Dim);
                g.DrawString(_g.Resolved, Ui.Small, b,
                    new RectangleF(12, Height - 36, Width - 24, 14), sf);

                TextRenderer.DrawText(g,
                    string.Format(L10n.T(_accts == 1 ? "card.account" : "card.accounts"), _accts), Ui.Small,
                    new Rectangle(12, Height - 28, Width - 24, 16), Ui.Accent,
                    TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, L10n.T("card.notfound"), Ui.Small,
                    new Rectangle(12, 40, Width - 24, 14), Ui.Faint,
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
        public string Placeholder { get; set; } = "";
        public event Action<int> Changed;

        int _sel = -1;
        Panel _shell;   // non-null while the dropdown is open
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
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
                ItemHeight = 26,
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
                TextRenderer.DrawText(e.Graphics, _lb.Items[e.Index].ToString(), Ui.Body,
                    new Point(e.Bounds.X + 12, e.Bounds.Y + 6),
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
            int h = Math.Min(Items.Count, 6) * 26 + 2;
            _shell.Size = new Size(Width, h);

            // dock below us, or above us if the window would clip it
            Point ho = host.PointToClient(PointToScreen(new Point(0, 0)));
            bool below = ho.Y + Height + 2 + h <= host.ClientSize.Height;
            _shell.Location = below
                ? new Point(ho.X, ho.Y + Height + 2)
                : new Point(ho.X, ho.Y - 2 - h);
            host.Controls.Add(_shell);
            _shell.BringToFront();
            host.Click += OnOwnerClick;
            host.Deactivate += _deact;
        }

        void OnOwnerClick(object s, EventArgs e)
        {
            if (_shell == null) return;
            Point p = _shell.PointToClient(Cursor.Position);
            if (p.X < 0 || p.Y < 0 || p.X >= _shell.Width || p.Y >= _shell.Height)
                Close();
        }

        void Close()
        {
            if (_shell == null) return;
            var host = FindForm();
            if (host != null)
            {
                host.Click -= OnOwnerClick;
                host.Deactivate -= _deact;
            }
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
            TextRenderer.DrawText(g, t ?? Placeholder, Font,
                new Rectangle(10, 0, Width - 32, Height), c,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            // chevron
            using (var pen = new Pen(c))
            {
                int cx = Width - 17, cy = Height / 2;
                g.DrawLines(pen, new[]
                {
                    new Point(cx - 3, cy - 2), new Point(cx, cy + 2), new Point(cx + 3, cy - 2),
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
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);
            int s = 13, x = 1, y = (Height - s) / 2;
            using (var b = new SolidBrush(Checked ? Ui.AccentDim : Ui.Panel))
                g.FillRectangle(b, x, y, s, s);
            using (var p = new Pen(Checked ? Ui.Accent : Ui.Line))
                g.DrawRectangle(p, x, y, s, s);
            if (Checked)
                using (var p = new Pen(Ui.Text))
                    g.DrawLines(p, new[]
                    {
                        new Point(x + 3, y + 7), new Point(x + 5, y + 9), new Point(x + 9, y + 4),
                    });
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(x + s + 7, 0, Width - x - s - 8, Height),
                Checked ? Ui.Text : Ui.Dim,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
