// Official MET/yr.no weather symbols (SVG from metno/weathericons). GDI fallback.
using OpeningTimesWidget.Services;
using Svg;

namespace OpeningTimesWidget.Controls;

public static class WeatherGlyph
{
    private static readonly Dictionary<string, Bitmap> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "Assets", "weather");

    public static void Draw(Graphics g, Rectangle r, string symbol)
    {
        if (r.Width < 8 || r.Height < 8) return;
        var bmp = GetBitmap(symbol, Math.Max(r.Width, r.Height));
        if (bmp != null)
        {
            g.DrawImage(bmp, r);
            return;
        }
        DrawFallback(g, r, symbol);
    }

    private static Bitmap? GetBitmap(string symbol, int size)
    {
        size = Math.Clamp(size, 12, 128);
        var key = $"{symbol}|{size}";
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var hit))
                return hit;
        }

        var path = ResolveSvg(symbol);
        if (path == null) return null;
        try
        {
            var doc = SvgDocument.Open(path);
            var bmp = doc.Draw(size, size);
            lock (Cache) Cache[key] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveSvg(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || !Directory.Exists(Dir))
            return null;
        var candidates = new[]
        {
            symbol + ".svg",
            WeatherService.BaseSymbol(symbol) + ".svg",
            WeatherService.BaseSymbol(symbol) + "_day.svg"
        };
        foreach (var name in candidates)
        {
            var p = Path.Combine(Dir, name);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static void DrawFallback(Graphics g, Rectangle r, string symbol)
    {
        var kind = WeatherService.BaseSymbol(symbol);
        var night = symbol.Contains("night", StringComparison.OrdinalIgnoreCase);

        if (kind.Contains("thunder", StringComparison.Ordinal))
        {
            FillCloud(g, r, Color.FromArgb(0x5A, 0x62, 0x72));
            return;
        }
        if (kind.Contains("rain", StringComparison.Ordinal) || kind.Contains("sleet", StringComparison.Ordinal)
            || kind.Contains("snow", StringComparison.Ordinal))
        {
            FillCloud(g, r, Color.FromArgb(0x5C, 0x6B, 0x82));
            return;
        }
        if (kind is "cloudy" or "fog" or "partlycloudy")
        {
            FillCloud(g, r, Color.FromArgb(0x90, 0x98, 0xA8));
            return;
        }

        using var sun = new SolidBrush(night ? Color.FromArgb(0xE8, 0xEC, 0xF4) : Color.FromArgb(0xFF, 0xCC, 0x33));
        var pad = r.Width / 5;
        g.FillEllipse(sun, r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2);
    }

    private static void FillCloud(Graphics g, Rectangle r, Color c)
    {
        using var b = new SolidBrush(c);
        var y = r.Y + r.Height * 0.28f;
        var h = r.Height * 0.48f;
        g.FillEllipse(b, r.X + r.Width * 0.08f, y, r.Width * 0.46f, h);
        g.FillEllipse(b, r.X + r.Width * 0.32f, y - r.Height * 0.12f, r.Width * 0.42f, h * 1.05f);
        g.FillEllipse(b, r.X + r.Width * 0.48f, y + r.Height * 0.02f, r.Width * 0.42f, h);
    }
}
