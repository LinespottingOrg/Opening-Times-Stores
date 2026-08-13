// Owner-drawn row — name is a link (hand cursor → open store URL).
//   Line 1:  Store name (link) …………………  hours or "Closed"
//   Line 2:  ………………………………………  Closes in… / Opens tomorrow…
//   Line 3:  [special chips]
using System.Diagnostics;
using OpeningTimesWidget.Models;
using OpeningTimesWidget.Services;
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class StoreRowControl : Control
{
    public const int MinRowHeight = 92;
    public const int RowGap = 10;

    private ThemePalette _p = AppTheme.Dark;
    private string _name = "";
    private string _hours = "—";
    private string _status = "";
    private string? _url;
    private string? _special;
    private string? _sale;
    private bool _closed;
    private bool _alert;
    private TimerUrgency _urgency = TimerUrgency.None;
    private bool _nameHover;
    private Rectangle _nameHit = Rectangle.Empty;

    private static readonly Font NameFont = new("Segoe UI Semibold", 13f);
    private static readonly Font NameLinkFont = new("Segoe UI Semibold", 13f, FontStyle.Underline);
    private static readonly Font HoursFont = new("Segoe UI Semibold", 11.5f);
    private static readonly Font StatusFont = new("Segoe UI", 9.5f);
    private static readonly Font ChipFont = new("Segoe UI Semibold", 9f);

    public StoreRowControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable |
            ControlStyles.StandardClick,
            true);
        Height = MinRowHeight;
        TabStop = false;
    }

    public void Bind(StoreEntry store, int dayOfWeek, ThemePalette p)
    {
        _p = p;
        _name = store.Name ?? "";
        _url = string.IsNullOrWhiteSpace(store.Url)
            ? StoreCatalog.ResolveUrl(store.Name ?? "")
            : store.Url.Trim();

        var todayHours = NormalizeHours(HoursHelper.GetDayHours(store, dayOfWeek));
        var st = HoursHelper.GetStatus(store, dayOfWeek, DateTime.Now);
        _status = st.Text;
        _urgency = st.Urgency;

        if (st.Kind is OpenStatusKind.OpensTomorrow or OpenStatusKind.ClosedUnknown)
        {
            _closed = true;
            _hours = "Closed";
        }
        else
        {
            _closed = todayHours.Contains("Closed", StringComparison.OrdinalIgnoreCase);
            _hours = string.IsNullOrWhiteSpace(todayHours) ? "—" : todayHours;
        }

        _special = string.IsNullOrWhiteSpace(store.Special) ? null : store.Special.Trim();
        var alert = store.WatchedItems.FirstOrDefault(w => w.OnSale || w.PriceDrop);
        _alert = alert != null;
        _sale = alert == null
            ? null
            : (alert.PriceDrop ? "↓ " : "SALE ") +
              (string.IsNullOrWhiteSpace(alert.Query) ? "item" : alert.Query.Trim());

        Height = MeasureHeight(Math.Max(200, Width));
        Invalidate();
    }

    public void Relayout()
    {
        var h = MeasureHeight(Width);
        if (Height != h) Height = h;
        Invalidate();
    }

    private int MeasureHeight(int width)
    {
        _ = width;
        var y = 14 + 28;
        if (!string.IsNullOrEmpty(_status)) y += 20;
        if (_special != null || _sale != null) y += 8 + 24;
        y += 14;
        return Math.Max(MinRowHeight, y);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var h = MeasureHeight(Width);
        if (Height != h) Height = h;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var over = !string.IsNullOrWhiteSpace(_url) && _nameHit.Contains(e.Location);
        if (over != _nameHover)
        {
            _nameHover = over;
            Cursor = over ? Cursors.Hand : Cursors.Default;
            Invalidate(_nameHit);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_nameHover)
        {
            _nameHover = false;
            Cursor = Cursors.Default;
            Invalidate(_nameHit);
        }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left &&
            !string.IsNullOrWhiteSpace(_url) &&
            _nameHit.Contains(e.Location))
        {
            OpenUrl(_url!);
        }
        base.OnMouseClick(e);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore — no crash on bad URL
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

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

        const int padL = 14;
        const int padR = 14;
        const int gap = 12;
        var left = _alert ? 16 : padL;
        var right = Width - padR;
        var usable = Math.Max(80, right - left);

        var hoursColor = _closed ? _p.Closed : _p.TextPrimary;
        var hoursNeed = TextRenderer.MeasureText(g, _hours, HoursFont,
            new Size(400, 30), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + 6;
        var hoursW = Math.Clamp(hoursNeed, 56, Math.Min(140, usable / 3 + 20));
        var nameW = Math.Max(100, usable - hoursW - gap);

        // Name hit-box for link (only as wide as the text, not full column)
        var nameSize = TextRenderer.MeasureText(g, _name, NameFont,
            new Size(nameW, 30), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var nameTextW = Math.Min(nameW, nameSize.Width + 4);
        _nameHit = new Rectangle(left, 12, nameTextW, 28);

        // Link colour when URL present
        var hasLink = !string.IsNullOrWhiteSpace(_url);
        Color nameColor;
        if (!hasLink)
            nameColor = _p.TextPrimary;
        else if (_nameHover)
            nameColor = _p.AlertGreen;
        else
            nameColor = Color.FromArgb(
                Math.Min(255, (int)_p.TextPrimary.R),
                Math.Min(255, (int)_p.TextPrimary.G + 20),
                Math.Min(255, (int)_p.TextPrimary.B + 40)); // slight cool tint = clickable

        var nameFont = hasLink && _nameHover ? NameLinkFont : NameFont;
        var nameRect = new Rectangle(left, 12, nameW, 28);
        TextRenderer.DrawText(g, _name, nameFont, nameRect, nameColor,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

        // Subtle underline when link (always when has URL, stronger on hover)
        if (hasLink)
        {
            var underlineY = 12 + 22;
            using var pen = new Pen(_nameHover ? _p.AlertGreen : Color.FromArgb(80, nameColor), _nameHover ? 1.5f : 1f);
            g.DrawLine(pen, left, underlineY, left + nameTextW - 2, underlineY);
        }

        var hoursRect = new Rectangle(right - hoursW, 12, hoursW, 28);
        TextRenderer.DrawText(g, _hours, HoursFont, hoursRect, hoursColor,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

        var y = 40;
        if (!string.IsNullOrEmpty(_status))
        {
            // Green (>45m) → yellow (≤45) → orange (≤40) → red (≤30)
            var statusColor = _urgency switch
            {
                TimerUrgency.Green => _p.AlertGreen,
                TimerUrgency.Yellow => _p.OpensTomorrow,
                TimerUrgency.Orange => _p.TimerOrange,
                TimerUrgency.Red => _p.TimerRed,
                TimerUrgency.OpensTomorrow => _p.OpensTomorrow,
                _ => _p.TextSecondary
            };

            var statusRect = new Rectangle(left, y, usable, 20);
            TextRenderer.DrawText(g, _status, StatusFont, statusRect, statusColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            y += 20;
        }

        if (_special != null || _sale != null)
        {
            y += 6;
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
        return h;
    }
}
