// Suggestion dropdown — card style, keyboard + mouse, no form-height side effects.
using OpeningTimesWidget.Services;
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class SuggestionList : Control
{
    public const int RowH = 48;
    public event EventHandler<SearchHit>? SuggestionChosen;

    private ThemePalette _p = AppTheme.Dark;
    private List<SearchHit> _hits = new();
    private int _selected;
    private int _hover = -1;
    private int _maxVisible = 6;

    private static readonly Font NameFont = new("Segoe UI Semibold", 11f);
    private static readonly Font SubFont = new("Segoe UI", 8.5f);
    private static readonly Font BadgeFont = new("Segoe UI Semibold", 7.5f);

    public SuggestionList()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        Height = 0;
        Visible = false;
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    public bool HasItems => _hits.Count > 0;

    public SearchHit? SelectedHit =>
        _selected >= 0 && _selected < _hits.Count ? _hits[_selected] : null;

    public void ApplyTheme(ThemePalette p)
    {
        _p = p;
        Invalidate();
    }

    public void ShowHits(IReadOnlyList<SearchHit> hits, int maxVisible = 6)
    {
        _hits = hits.ToList();
        _maxVisible = Math.Max(1, maxVisible);
        _selected = _hits.Count > 0 ? 0 : -1;
        _hover = -1;

        if (_hits.Count == 0)
        {
            HideList();
            return;
        }

        var rows = Math.Min(_maxVisible, _hits.Count);
        Height = rows * RowH + 4; // padding + border
        Visible = true;
        BringToFront();
        Invalidate();
    }

    public void HideList()
    {
        _hits.Clear();
        _selected = -1;
        _hover = -1;
        Height = 0;
        Visible = false;
    }

    public void MoveSelection(int delta)
    {
        if (_hits.Count == 0) return;
        _selected = Math.Clamp(_selected + delta, 0, _hits.Count - 1);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var i = IndexAt(e.Y);
        if (i != _hover)
        {
            _hover = i;
            if (i >= 0) _selected = i;
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var i = IndexAt(e.Y);
        if (i >= 0 && i < _hits.Count)
        {
            _selected = i;
            SuggestionChosen?.Invoke(this, _hits[i]);
        }
        base.OnMouseClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down) { MoveSelection(1); e.Handled = true; }
        else if (e.KeyCode == Keys.Up) { MoveSelection(-1); e.Handled = true; }
        else if (e.KeyCode is Keys.Enter or Keys.Tab)
        {
            if (SelectedHit is { } hit)
                SuggestionChosen?.Invoke(this, hit);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideList();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private int IndexAt(int y)
    {
        var i = (y - 2) / RowH;
        if (i < 0 || i >= Math.Min(_maxVisible, _hits.Count)) return -1;
        return i;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_hits.Count == 0) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var parentBg = Parent?.BackColor ?? _p.WindowBg;
        g.Clear(parentBg);

        var card = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var b = new SolidBrush(_p.Surface))
            UiDraw.FillRound(g, b, card, 10);
        using (var pen = new Pen(_p.Border))
            UiDraw.DrawRound(g, pen, card, 10);

        var visible = Math.Min(_maxVisible, _hits.Count);
        for (var i = 0; i < visible; i++)
        {
            var hit = _hits[i];
            var row = new Rectangle(2, 2 + i * RowH, Width - 5, RowH - 1);
            var active = i == _selected || i == _hover;

            if (active)
            {
                using var b = new SolidBrush(_p.AlertGreenBg);
                UiDraw.FillRound(g, b, row, 8);
            }

            // Left rail for favorite / selection
            if (hit.IsFavorite || active)
            {
                using var rail = new SolidBrush(hit.IsFavorite ? _p.AlertGreen : _p.Special);
                g.FillRectangle(rail, row.X + 4, row.Y + 10, 3, row.Height - 20);
            }

            var textLeft = row.X + 14;
            var textW = row.Width - 20;

            // Name
            var nameFg = active ? _p.AlertGreen : _p.TextPrimary;
            var name = hit.DisplayName;
            TextRenderer.DrawText(g, name, NameFont,
                new Rectangle(textLeft, row.Y + 6, textW - (hit.IsFavorite ? 36 : 0), 20),
                nameFg, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);

            // ★ badge
            if (hit.IsFavorite)
            {
                var badge = "★";
                TextRenderer.DrawText(g, badge, BadgeFont,
                    new Rectangle(row.Right - 28, row.Y + 8, 20, 16),
                    _p.AlertGreen, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }

            // Subtitle
            var sub = hit.Subtitle ?? "";
            if (!string.IsNullOrEmpty(hit.MatchedOn))
                sub = string.IsNullOrEmpty(sub) ? hit.MatchedOn : sub + "  ·  " + hit.MatchedOn;

            TextRenderer.DrawText(g, sub, SubFont,
                new Rectangle(textLeft, row.Y + 26, textW, 16),
                _p.TextSecondary, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // Divider between rows
            if (i < visible - 1)
            {
                using var div = new Pen(Color.FromArgb(40, _p.Border));
                g.DrawLine(div, row.X + 12, row.Bottom, row.Right - 12, row.Bottom);
            }
        }
    }
}
