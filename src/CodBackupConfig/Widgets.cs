using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodBackupConfig
{
    // flat button, 1px border, hover shifts the border to the accent color
    class LineButton : Button
    {
        bool _hot;
        protected bool Hot => _hot;
        public bool Highlight { get; set; }

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

            int fw = 22, fh = 16;
            if (Flag != null) Img.Fit(g, Flag, new Rectangle(8, (Height - fh) / 2, fw, fh));
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(8 + fw + 6, 0, Width - 8 - fw - 14, Height),
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
            Font = Ui.Label;
            ForeColor = Ui.Faint;
            BackColor = Color.Transparent;
        }
    }

    // clickable game card: cover, detected path, account count
    class GameCard : Control
    {
        const int Slot = 84; // cover width

        GameInfo _g;
        Bitmap _cover;
        bool _hot;
        Button _browse;
        bool _found;
        int _accts;

        public event Action Clicked;
        public event Action<GameInfo> BrowseClick;
        public bool Selected { get; private set; }
        public Button BrowseButton => _browse;

        public GameCard(GameInfo g)
        {
            _g = g;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _cover = Assets.Img(g.Key + ".jpg");

            _browse = new Button
            {
                Visible = false,
                Font = Ui.Small,
                ForeColor = Ui.Dim,
                BackColor = Ui.Panel,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
            };
            _browse.FlatAppearance.BorderSize = 0;
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
            _browse.Bounds = new Rectangle(Width - 134, Height - 24, 122, 18);

        protected override void OnLayout(LayoutEventArgs e) => LayoutBrowse();

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(_hot ? Ui.PanelHi : Ui.Panel))
                g.FillRectangle(b, ClientRectangle);
            using (var p = new Pen(Selected ? Ui.Accent : _hot ? Ui.Faint : Ui.Line))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            Img.Fit(g, _cover, new Rectangle(8, 8, Slot - 16, Height - 16));
            using (var sep = new Pen(Ui.Line))
                g.DrawLine(sep, Slot, 8, Slot, Height - 8);

            TextRenderer.DrawText(g, _g.Name, Ui.CardName,
                new Rectangle(Slot + 12, 12, Width - Slot - 22, 18), Ui.Text,
                TextFormatFlags.NoPadding);

            if (_found)
            {
                using var sf = new StringFormat(StringFormatFlags.LineLimit);
                sf.Trimming = StringTrimming.EllipsisCharacter;
                using var b = new SolidBrush(Ui.Dim);
                g.DrawString(_g.Resolved, Ui.Small, b,
                    new RectangleF(Slot + 12, 34, Width - Slot - 24, 14), sf);
                TextRenderer.DrawText(g,
                    string.Format(L10n.T("card.accounts"), _accts), Ui.Small,
                    new Rectangle(Slot + 12, Height - 28, Width - Slot - 22, 16), Ui.Accent,
                    TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, L10n.T("card.notfound"), Ui.Small,
                    new Rectangle(Slot + 12, Height - 28, Width - Slot - 22, 16), Ui.Faint,
                    TextFormatFlags.NoPadding);
            }
        }
    }
}
