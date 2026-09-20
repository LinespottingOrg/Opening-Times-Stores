// Store card — all hours live in the widget (today + week + status). No web links.
using OpeningTimesWidget.Models;
using OpeningTimesWidget.Services;
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class StoreRowControl : Control
{
    public const int MinRowHeight = 120;
    public const int RowGap = 10;

    private ThemePalette _p = AppTheme.Dark;
    private string _name = "";
    private string _hours = "—";
    private string _status = "";
    private string _week = "";
    private string? _special;
    private string? _sale;
    private bool _closed;
    private bool _alert;
    private TimerUrgency _urgency = TimerUrgency.None;

    private static readonly Font NameFont = new("Segoe UI Semibold", 13.5f);
    private static readonly Font HoursFont = new("Segoe UI Semibold", 14f);
    private static readonly Font StatusFont = new("Segoe UI", 9.5f);
    private static readonly Font WeekFont = new("Segoe UI", 8.5f);
    private static readonly Font ChipFont = new("Segoe UI Semibold", 9f);

    public StoreRowControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        Height = MinRowHeight;
        TabStop = false;
        Cursor = Cursors.Default;
    }

    public void Bind(StoreEntry store, int dayOfWeek, ThemePalette p, WeatherToday? wx = null)
    {
        _ = wx;
        _p = p;
        _name = store.Name ?? "";
        _week = HoursHelper.FormatWeek(store);

        var todayHours = NormalizeHours(HoursHelper.GetDayHours(store, dayOfWeek));
        var st = HoursHelper.GetStatus(store, dayOfWeek, DateTime.Now);
        _status = st.Text;
        _urgency = st.Urgency;

        if (st.Kind is OpenStatusKind.OpensTomorrow or OpenStatusKind.ClosedUnknown)
        {
            _closed = true;
            _hours = todayHours.Contains("stäng", StringComparison.OrdinalIgnoreCase) ||
                     todayHours.Contains("Closed", StringComparison.OrdinalIgnoreCase)
                ? "Stängt"
                : (string.IsNullOrWhiteSpace(todayHours) ? "Stängt" : todayHours);
        }
        else
        {
            _closed = todayHours.Contains("Closed", StringComparison.OrdinalIgnoreCase) ||
                      todayHours.Contains("stäng", StringComparison.OrdinalIgnoreCase);
            _hours = string.IsNullOrWhiteSpace(todayHours) ? "—" : todayHours;
            if (_closed) _hours = "Stängt";
        }

        _special = string.IsNullOrWhiteSpace(store.Special) ? null : store.Special.Trim();
        var alert = store.WatchedItems.FirstOrDefault(w => w.OnSale || w.PriceDrop);
        _alert = alert != null;
        _sale = alert == null
            ? null
            : (alert.PriceDrop ? "↓ " : "REA ") +
              (string.IsNullOrWhiteSpace(alert.Query) ? "vara" : alert.Query.Trim());

        Height = MeasureHeight();
        Invalidate();
    }

    public void Relayout()
    {
        var h = MeasureHeight();
        if (Height != h) Height = h;
        Invalidate();
    }

    private int MeasureHeight()
    {
        var y = 14 + 36;
        if (!string.IsNullOrEmpty(_status)) y += 24;
        y += 24;
        if (_special != null || _sale != null) y += 8 + 24;
        y += 16;
        return Math.Max(MinRowHeight, y);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var h = MeasureHeight();
        if (Height != h) Height = h;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var parentBg = Parent?.BackColor ?? _p.WindowBg;
        g.Clear(parentBg);

        var card = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using (var b = new SolidBrush(_p.Surface))
            UiDraw.FillRound(g, b, card, 10);
        using (var pen = new Pen(_p.Border))
            UiDraw.DrawRound(g, pen, card, 10);

        if (_alert)
        {
            using var rail = new SolidBrush(_p.AlertGreen);
            g.FillRectangle(rail, 3, 14, 4, Math.Max(24, Height - 28));
        }

        const int padL = 16;
        const int padR = 16;
        const int gap = 12;
        var left = _alert ? 18 : padL;
        var right = Width - padR;
        var usable = Math.Max(80, right - left);

        var hoursColor = _closed ? _p.Closed : _p.TextPrimary;
        var nameH = UiText.Line(g, NameFont);
        var hoursH = UiText.Line(g, HoursFont);
        var rowH = Math.Max(nameH, hoursH);
        var hoursNeed = UiText.Measure(g, _hours, HoursFont).Width + 8;
        var hoursW = Math.Clamp(hoursNeed, 88, Math.Min(220, Math.Max(hoursNeed, 96)));
        var nameW = Math.Max(80, usable - hoursW - gap);

        UiText.Draw(g, _name, NameFont, new Rectangle(left, 10, nameW, rowH), _p.TextPrimary);
        UiText.Draw(g, _hours, HoursFont, new Rectangle(right - hoursW, 10, hoursW, rowH), hoursColor,
            TextFormatFlags.Right);

        var y = 10 + rowH + 4;
        if (!string.IsNullOrEmpty(_status))
        {
            var statusColor = _urgency switch
            {
                TimerUrgency.Green => _p.AlertGreen,
                TimerUrgency.Yellow => _p.OpensTomorrow,
                TimerUrgency.Orange => _p.TimerOrange,
                TimerUrgency.Red => _p.TimerRed,
                TimerUrgency.OpensTomorrow => _p.OpensTomorrow,
                _ => _p.TextSecondary
            };
            var stH = UiText.Line(g, StatusFont);
            UiText.Draw(g, _status, StatusFont, new Rectangle(left, y, usable, stH), statusColor,
                TextFormatFlags.Right);
            y += stH + 2;
        }

        var weekH = UiText.Line(g, WeekFont);
        UiText.Draw(g, _week, WeekFont, new Rectangle(left, y, usable, weekH), _p.TextSecondary);
        y += weekH + 4;

        if (_special != null || _sale != null)
        {
            y += 4;
            var x = left;
            if (_special != null)
                x = DrawChip(g, _special, _p.Special, _p.SpecialBg, x, y);
            if (_sale != null)
                DrawChip(g, _sale, _p.AlertGreen, _p.AlertGreenBg, x, y);
        }
    }

    private static int DrawChip(Graphics g, string text, Color fg, Color bg, int x, int y)
    {
        var size = TextRenderer.MeasureText(g, text, ChipFont,
            new Size(400, 24), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var w = size.Width + 16;
        var r = new Rectangle(x, y, w, 22);
        using (var b = new SolidBrush(bg))
            UiDraw.FillRound(g, b, r, 6);
        TextRenderer.DrawText(g, text, ChipFont, r, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        return x + w + 8;
    }

    private static string NormalizeHours(string h)
    {
        if (string.IsNullOrWhiteSpace(h)) return "—";
        h = h.Trim();
        h = h.Replace(" - ", "–").Replace("-", "–").Replace("—", "–").Replace("~", "–");
        while (h.Contains("––"))
            h = h.Replace("––", "–");
        if (h.Contains("Closed", StringComparison.OrdinalIgnoreCase))
            return "Stängt";
        return h;
    }
}
