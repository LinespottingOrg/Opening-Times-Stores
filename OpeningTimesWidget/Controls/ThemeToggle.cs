// Segmented Light | Dark control — matches Claude prototype header.
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class ThemeToggle : Control
{
    private ThemeMode _mode = ThemeMode.Dark;
    private ThemePalette _palette = AppTheme.Dark;
    private int _hot = -1; // 0=light 1=dark

    public event EventHandler? ThemeChanged;

    public ThemeMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            Invalidate();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyPalette(ThemePalette p)
    {
        _palette = p;
        Invalidate();
    }

    public ThemeToggle()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(132, 30); // full "Light" / "Dark" labels
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var half = Width / 2;
        var hot = e.X < half ? 0 : 1;
        if (hot != _hot) { _hot = hot; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hot = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        Mode = e.X < Width / 2 ? ThemeMode.Light : ThemeMode.Dark;
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? _palette.Header);

        var outer = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var b = new SolidBrush(_palette.Surface))
            UiDraw.FillRound(g, b, outer, 8);
        using (var pen = new Pen(_palette.Border))
            UiDraw.DrawRound(g, pen, outer, 8);

        var half = Width / 2;
        var lightR = new Rectangle(2, 2, half - 3, Height - 5);
        var darkR = new Rectangle(half + 1, 2, half - 4, Height - 5);

        // Active pill
        var active = _mode == ThemeMode.Light ? lightR : darkR;
        using (var b = new SolidBrush(_palette.AlertGreenBg))
            UiDraw.FillRound(g, b, active, 6);
        using (var pen = new Pen(_palette.AlertGreen))
            UiDraw.DrawRound(g, pen, active, 6);

        using var font = new Font("Segoe UI Semibold", 8f);
        DrawSeg(g, "Light", lightR, _mode == ThemeMode.Light, _hot == 0, font);
        DrawSeg(g, "Dark", darkR, _mode == ThemeMode.Dark, _hot == 1, font);
    }

    private void DrawSeg(Graphics g, string text, Rectangle r, bool on, bool hot, Font font)
    {
        var fg = on ? _palette.AlertGreen : (hot ? _palette.TextPrimary : _palette.TextSecondary);
        TextRenderer.DrawText(g, text, font, r, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
