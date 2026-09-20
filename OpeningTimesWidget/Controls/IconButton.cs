// Circular / soft icon button matching Claude prototype header chrome.
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class IconButton : Control
{
    private ThemePalette _p = AppTheme.Dark;
    private bool _hover;
    private bool _pressed;

    public string Glyph { get; set; } = "⋯";
    public float GlyphSize { get; set; } = 12f;
    public bool IsAccent { get; set; }

    public IconButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(32, 32);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public void ApplyTheme(ThemePalette p)
    {
        _p = p;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? _p.Header);

        var r = new Rectangle(1, 1, Width - 3, Height - 3);
        Color fill;
        if (_pressed) fill = _p.Border;
        else if (_hover) fill = Color.FromArgb(40, _p.TextPrimary);
        else fill = Color.FromArgb(18, _p.TextPrimary);

        using (var b = new SolidBrush(fill))
            UiDraw.FillRound(g, b, r, 8);

        using var font = new Font("Segoe UI Symbol", GlyphSize, FontStyle.Regular);
        var fg = IsAccent ? _p.AlertGreen : _p.TextSecondary;
        if (_hover) fg = _p.TextPrimary;
        TextRenderer.DrawText(g, Glyph, font, ClientRectangle, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

public static class UiDraw
{
    public static void FillRound(Graphics g, Brush brush, Rectangle r, int radius)
    {
        using var path = Rounded(r, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRound(Graphics g, Pen pen, Rectangle r, int radius)
    {
        using var path = Rounded(r, radius);
        g.DrawPath(pen, path);
    }

    public static System.Drawing.Drawing2D.GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = Math.Max(2, radius * 2);
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>GDI text that never clips half a glyph — rect is always at least MeasureText tall.</summary>
public static class UiText
{
    public const TextFormatFlags BaseFlags = TextFormatFlags.NoPrefix;

    public static Size Measure(Graphics g, string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) text = "Åj";
        return TextRenderer.MeasureText(g, text, font, new Size(short.MaxValue, short.MaxValue),
            BaseFlags | TextFormatFlags.SingleLine);
    }

    public static int Line(Graphics g, Font font) => Measure(g, "Åy", font).Height;

    public static void Draw(Graphics g, string text, Font font, Rectangle r, Color color, TextFormatFlags extra = 0)
    {
        var need = Measure(g, text, font);
        if (r.Height < need.Height)
            r = new Rectangle(r.X, r.Y, r.Width, need.Height);
        if (r.Width < 4) return;
        TextRenderer.DrawText(g, text ?? "", font, r, color,
            BaseFlags | TextFormatFlags.EndEllipsis | extra);
    }
}
