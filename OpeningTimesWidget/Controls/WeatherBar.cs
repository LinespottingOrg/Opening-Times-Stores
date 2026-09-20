// Weather lives in the widget. Click a place to show its hours here — never opens a browser.
using OpeningTimesWidget.Services;
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class WeatherBar : Control
{
    public const int PanelWidth = 520;

    private ThemePalette _p = AppTheme.Dark;
    private IReadOnlyList<WeatherToday> _places = [WeatherToday.Empty("Stora Frö")];
    private SunDay _sun = new();
    private int _selected;
    private readonly List<Rectangle> _hits = new();

    private static readonly Font TempFont = new("Segoe UI Semibold", 32f);
    private static readonly Font PlaceFont = new("Segoe UI Semibold", 13f);
    private static readonly Font HintFont = new("Segoe UI Semibold", 11f);
    private static readonly Font HourFont = new("Segoe UI", 9.5f);
    private static readonly Font HourTempFont = new("Segoe UI Semibold", 11f);
    private static readonly Font AttrFont = new("Segoe UI", 10.5f);
    private static readonly Font CaptionFont = new("Segoe UI Semibold", 10f);

    public WeatherBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.StandardClick,
            true);
        Width = PanelWidth;
        Dock = DockStyle.Fill;
        Cursor = Cursors.Default;
        TabStop = false;
    }

    public void Bind(WeatherToday wx, SunDay sun, ThemePalette p) =>
        Bind(wx is null ? Array.Empty<WeatherToday>() : new[] { wx }, sun, p);

    public void Bind(IReadOnlyList<WeatherToday> places, SunDay sun, ThemePalette p)
    {
        _places = places.Count > 0 ? places : [WeatherToday.Empty("Stora Frö")];
        if (_selected >= _places.Count) _selected = 0;
        _sun = sun;
        _p = p;
        Invalidate();
    }

    private WeatherToday Selected => _places[Math.Clamp(_selected, 0, _places.Count - 1)];

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var over = _hits.Any(r => r.Contains(e.Location));
        Cursor = over ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            for (var i = 0; i < _hits.Count; i++)
            {
                if (_hits[i].Contains(e.Location))
                {
                    _selected = i;
                    Invalidate();
                    break;
                }
            }
        }
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(_p.Header);
        using (var edge = new Pen(_p.Border))
            g.DrawLine(edge, 0, 0, 0, Height);

        const int pad = 14;
        _hits.Clear();
        var y = pad;
        var innerW = Math.Max(120, Width - pad * 2);

        var capH = UiText.Line(g, CaptionFont);
        UiText.Draw(g, "Väder", CaptionFont, new Rectangle(pad, y, innerW, capH), _p.TextSecondary);
        y += capH + 10;

        var cardH = PlaceCardHeight(g);
        for (var i = 0; i < _places.Count; i++)
        {
            var card = new Rectangle(pad, y, innerW, cardH);
            DrawPlaceCard(g, _places[i], card, selected: i == _selected);
            _hits.Add(card);
            y += cardH + 12;
        }

        var sel = Selected;
        var hintColor = OutdoorColor(sel.OutdoorHint, _p);
        var hint = string.IsNullOrWhiteSpace(sel.OutdoorHint)
            ? $"Ute · {sel.Place}"
            : $"{sel.OutdoorHint}  ·  {sel.Place}";
        var hintH = UiText.Line(g, HintFont);
        UiText.Draw(g, hint, HintFont, new Rectangle(pad, y, innerW, hintH), hintColor);
        y += hintH + 8;

        var sunTxt =
            $"Sol  ↑ {(_sun.Sunrise?.ToString("HH:mm") ?? "—")}     ↓ {(_sun.Sunset?.ToString("HH:mm") ?? "—")}";
        var sunH = UiText.Line(g, PlaceFont);
        UiText.Draw(g, sunTxt, PlaceFont, new Rectangle(pad, y, innerW, sunH), _p.OpensTomorrow);
        y += sunH + 10;

        var hoursCap = $"Kommande timmar · {sel.Place}";
        UiText.Draw(g, hoursCap, CaptionFont, new Rectangle(pad, y, innerW, capH), _p.TextSecondary);
        y += capH + 8;

        var hourlyH = Math.Max(120, Height - y - pad);
        DrawHourly(g, new Rectangle(pad, y, innerW, hourlyH), sel);
    }

    private int PlaceCardHeight(Graphics g)
    {
        return 16 + UiText.Line(g, PlaceFont) + UiText.Line(g, TempFont) + UiText.Line(g, AttrFont) + 20;
    }

    private void DrawPlaceCard(Graphics g, WeatherToday wx, Rectangle card, bool selected)
    {
        using (var b = new SolidBrush(selected ? Color.FromArgb(40, _p.DayStrip) : _p.Surface))
            UiDraw.FillRound(g, b, card, 12);
        using (var pen = new Pen(selected ? _p.DayStrip : _p.Border, selected ? 2f : 1f))
            UiDraw.DrawRound(g, pen, card, 12);

        var icon = Math.Min(80, card.Height - 24);
        WeatherGlyph.Draw(g, new Rectangle(card.X + 14, card.Y + (card.Height - icon) / 2, icon, icon), wx.NowSymbol);

        var textX = card.X + 14 + icon + 14;
        var textW = Math.Max(40, card.Right - 14 - textX);
        var y = card.Y + 10;

        var placeH = UiText.Line(g, PlaceFont);
        UiText.Draw(g, wx.Place, PlaceFont, new Rectangle(textX, y, textW, placeH), _p.TextPrimary);
        y += placeH + 2;

        var temp = wx.Error != null ? "—" : $"{wx.NowTemp:0}°";
        var tempColor = TempColor(wx.NowTemp);
        var tempH = UiText.Line(g, TempFont);
        var tempSize = UiText.Measure(g, temp, TempFont);
        UiText.Draw(g, temp, TempFont, new Rectangle(textX, y, tempSize.Width + 8, tempH), tempColor);

        var extra = wx.Error == null ? $"{wx.NowPrecipMm:0.#} mm   {wx.NowWindMs:0} m/s" : "";
        if (extra.Length > 0)
        {
            var extraH = UiText.Line(g, AttrFont);
            UiText.Draw(g, extra, AttrFont,
                new Rectangle(textX + tempSize.Width + 16, y + (tempH - extraH) / 2,
                    Math.Max(40, textW - tempSize.Width - 16), extraH), _p.TextSecondary);
        }
        y += tempH + 4;

        var line = wx.Error ?? $"{wx.ConditionSv}   {wx.TodayMin:0}° / {wx.TodayMax:0}°";
        var lineH = UiText.Line(g, AttrFont);
        UiText.Draw(g, line, AttrFont, new Rectangle(textX, y, textW, lineH), _p.TextSecondary);
    }

    private void DrawHourly(Graphics g, Rectangle box, WeatherToday wx)
    {
        var hours = wx.Hours;
        if (hours.Count == 0)
        {
            TextRenderer.DrawText(g, "Ingen timprognos ännu", AttrFont, box, _p.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            return;
        }

        const int cols = 4;
        var hhH = UiText.Line(g, HourFont);
        var tH = UiText.Line(g, HourTempFont);
        var icon = 32;
        var cellH = Math.Max(80, hhH + tH + icon + 16);
        var cellW = Math.Max(70, box.Width / cols);
        var nowH = wx.NowLocal == default ? DateTime.Now.Hour : wx.NowLocal.Hour;

        for (var i = 0; i < hours.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var cell = new Rectangle(box.X + col * cellW, box.Y + row * cellH, cellW - 6, cellH - 6);
            if (cell.Bottom > box.Bottom + 2) break;
            var h = hours[i];
            if (h.Local.Hour == nowH)
            {
                using var mark = new SolidBrush(Color.FromArgb(32, _p.DayStrip));
                UiDraw.FillRound(g, mark, cell, 8);
            }

            var y = cell.Y + 4;
            UiText.Draw(g, h.Local.ToString("HH"), HourFont,
                new Rectangle(cell.X, y, cell.Width, hhH), _p.TextSecondary, TextFormatFlags.HorizontalCenter);
            y += hhH + 2;
            WeatherGlyph.Draw(g, new Rectangle(cell.X + (cell.Width - icon) / 2, y, icon, icon), h.Symbol);
            y += icon + 2;
            var tColor = TempColor(h.TempC);
            UiText.Draw(g, $"{h.TempC:0}°", HourTempFont,
                new Rectangle(cell.X, y, cell.Width, tH), tColor, TextFormatFlags.HorizontalCenter);
        }
    }

    private Color TempColor(double celsius) =>
        celsius < 0 ? _p.TempMinus : _p.TempPlus;

    private static Color OutdoorColor(string hint, ThemePalette p)
    {
        if (hint.StartsWith("Sol", StringComparison.OrdinalIgnoreCase))
            return p.OpensTomorrow;
        if (hint.StartsWith("Delvis", StringComparison.OrdinalIgnoreCase))
            return p.DayStrip;
        if (hint.Contains("Regn", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("dåligt", StringComparison.OrdinalIgnoreCase))
            return p.Closed;
        return p.TextSecondary;
    }
}
