// Today's forecast from MET Norway Locationforecast 2.0 (the same data yr.no uses).
// Identify with User-Agent + contact. Cache Expires / If-Modified-Since per TOS.
using System.Globalization;
using System.Text.Json;

namespace OpeningTimesWidget.Services;

public sealed class WeatherHour
{
    public DateTime Local { get; init; }
    public double TempC { get; init; }
    public string Symbol { get; init; } = "";
    public double PrecipMm { get; init; }
    public double CloudPct { get; init; }
    public double WindMs { get; init; }
}

public sealed class WeatherToday
{
    public string Place { get; init; } = "Stora Frö";
    public DateTime NowLocal { get; init; }
    public double NowTemp { get; init; }
    public string NowSymbol { get; init; } = "";
    public double NowWindMs { get; init; }
    public double NowPrecipMm { get; init; }
    public double TodayMin { get; init; }
    public double TodayMax { get; init; }
    public IReadOnlyList<WeatherHour> Hours { get; init; } = Array.Empty<WeatherHour>();
    public string OutdoorHint { get; init; } = "";
    public string ConditionSv { get; init; } = "";
    public string? Error { get; init; }
    public string YrUrl { get; init; } = "https://www.yr.no/";

    public static WeatherToday Empty(string place, string? error = null) => new()
    {
        Place = place,
        Error = error ?? "väder —"
    };
}

public static class WeatherService
{
    public const string UserAgent = "OpeningTimesEU/1.0.2 (info@linespotting.com)";
    private const string Api = "https://api.met.no/weatherapi/locationforecast/2.0/compact";

    private static readonly HttpClient Http = CreateClient();
    private static readonly object Gate = new();
    private static WeatherToday? _latest;
    private static readonly List<WeatherToday> _latestAll = new();
    private static readonly Dictionary<string, CacheSlot> Cache = new(StringComparer.Ordinal);

    private sealed class CacheSlot
    {
        public string Body = "";
        public DateTime ExpiresUtc;
        public string? LastModified;
    }

    public static WeatherToday? Latest
    {
        get { lock (Gate) return _latest; }
    }

    public static IReadOnlyList<WeatherToday> LatestAll
    {
        get { lock (Gate) return _latestAll.ToList(); }
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        return http;
    }

    public static async Task<IReadOnlyList<WeatherToday>> GetPlacesAsync(
        IEnumerable<Models.WeatherPlace> places, CancellationToken ct = default)
    {
        var list = places.ToList();
        if (list.Count == 0)
            list = Models.WeatherPlace.DavidDefaults.ToList();
        var tasks = list.Select(p => GetTodayAsync(p.Name, p.Lat, p.Lon, p.Timezone, ct, p.YrUrl)).ToList();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        lock (Gate)
        {
            _latestAll.Clear();
            _latestAll.AddRange(results);
            _latest = results.FirstOrDefault();
        }
        return results;
    }

    public static async Task<WeatherToday> GetTodayAsync(
        string place, double lat, double lon, string? timezone, CancellationToken ct = default, string? yrUrl = null)
    {
        place = string.IsNullOrWhiteSpace(place) ? "Stora Frö" : place.Trim();
        lat = Math.Round(lat, 4);
        lon = Math.Round(lon, 4);
        var tz = ResolveTz(timezone);
        var url =
            $"{Api}?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}";

        try
        {
            var body = await FetchJsonAsync(url, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
                return Stamp(WeatherToday.Empty(place, "yr.no svarade tomt"));

            using var doc = JsonDocument.Parse(body);
            var parsed = Parse(doc.RootElement, place, lat, lon, tz, yrUrl);
            return Stamp(parsed);
        }
        catch (Exception ex)
        {
            var fallback = Latest;
            if (fallback != null && fallback.Error == null)
                return fallback;
            return Stamp(WeatherToday.Empty(place, ShortErr(ex)));
        }
    }

    private static WeatherToday Stamp(WeatherToday w)
    {
        lock (Gate) _latest = w;
        return w;
    }

    private static async Task<string?> FetchJsonAsync(string url, CancellationToken ct)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(url, out var hit) && DateTime.UtcNow < hit.ExpiresUtc && hit.Body.Length > 0)
                return hit.Body;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        lock (Gate)
        {
            if (Cache.TryGetValue(url, out var slot) && !string.IsNullOrWhiteSpace(slot.LastModified))
                req.Headers.TryAddWithoutValidation("If-Modified-Since", slot.LastModified);
        }

        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);

        if (resp.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            lock (Gate)
            {
                if (Cache.TryGetValue(url, out var slot))
                {
                    TouchExpiry(resp, slot);
                    return slot.Body;
                }
            }
        }

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        lock (Gate)
        {
            var slot = new CacheSlot { Body = json };
            TouchExpiry(resp, slot);
            slot.LastModified = resp.Content.Headers.LastModified?.ToString("R")
                                ?? resp.Headers.Date?.ToString("R");
            Cache[url] = slot;
            TryWriteDisk(url, json);
        }
        return json;
    }

    private static void TouchExpiry(HttpResponseMessage resp, CacheSlot slot)
    {
        var exp = resp.Content.Headers.Expires?.UtcDateTime;
        slot.ExpiresUtc = exp ?? DateTime.UtcNow.AddMinutes(20);
        if (slot.ExpiresUtc < DateTime.UtcNow.AddMinutes(5))
            slot.ExpiresUtc = DateTime.UtcNow.AddMinutes(10);
    }

    private static void TryWriteDisk(string url, string json)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpeningTimesWidget");
            Directory.CreateDirectory(dir);
            var name = "yr-" + Math.Abs(url.GetHashCode(StringComparison.Ordinal)).ToString("x") + ".json";
            File.WriteAllText(Path.Combine(dir, name), json);
        }
        catch { /* cache is optional */ }
    }

    private static WeatherToday Parse(JsonElement root, string place, double lat, double lon, TimeZoneInfo tz, string? yrUrl)
    {
        if (!root.TryGetProperty("properties", out var props) ||
            !props.TryGetProperty("timeseries", out var series) ||
            series.ValueKind != JsonValueKind.Array)
            return WeatherToday.Empty(place, "yr.no format");

        var hours = new List<WeatherHour>();
        foreach (var pt in series.EnumerateArray())
        {
            if (!pt.TryGetProperty("time", out var tEl)) continue;
            if (!DateTime.TryParse(tEl.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var utc))
                continue;
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

            if (!pt.TryGetProperty("data", out var data)) continue;
            double temp = 0, cloud = 0, precip = 0, wind = 0;
            var symbol = "";
            if (data.TryGetProperty("instant", out var instant) &&
                instant.TryGetProperty("details", out var details))
            {
                if (details.TryGetProperty("air_temperature", out var t)) temp = t.GetDouble();
                if (details.TryGetProperty("cloud_area_fraction", out var c)) cloud = c.GetDouble();
                if (details.TryGetProperty("wind_speed", out var w)) wind = w.GetDouble();
            }
            if (data.TryGetProperty("next_1_hours", out var n1))
            {
                if (n1.TryGetProperty("summary", out var sum) &&
                    sum.TryGetProperty("symbol_code", out var sc))
                    symbol = sc.GetString() ?? "";
                if (n1.TryGetProperty("details", out var d1) &&
                    d1.TryGetProperty("precipitation_amount", out var p))
                    precip = p.GetDouble();
            }
            else if (data.TryGetProperty("next_6_hours", out var n6) &&
                     n6.TryGetProperty("summary", out var sum6) &&
                     sum6.TryGetProperty("symbol_code", out var sc6))
            {
                symbol = sc6.GetString() ?? "";
            }

            hours.Add(new WeatherHour
            {
                Local = local,
                TempC = temp,
                Symbol = symbol,
                PrecipMm = precip,
                CloudPct = cloud,
                WindMs = wind
            });
        }

        if (hours.Count == 0)
            return WeatherToday.Empty(place, "ingen prognos");

        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var today = nowLocal.Date;
        var nowHour = hours
            .Where(h => h.Local <= nowLocal)
            .OrderByDescending(h => h.Local)
            .FirstOrDefault() ?? hours[0];

        var todayHours = hours
            .Where(h => h.Local.Date == today)
            .OrderBy(h => h.Local)
            .ToList();
        if (todayHours.Count == 0)
            todayHours = hours.Take(24).ToList();

        var temps = todayHours.Select(h => h.TempC).ToList();
        var strip = hours
            .Where(h => h.Local >= nowLocal.AddMinutes(-20))
            .OrderBy(h => h.Local)
            .Take(12)
            .ToList();
        if (strip.Count == 0) strip = todayHours;

        return new WeatherToday
        {
            Place = place,
            NowLocal = nowLocal,
            NowTemp = nowHour.TempC,
            NowSymbol = nowHour.Symbol,
            NowWindMs = nowHour.WindMs,
            NowPrecipMm = nowHour.PrecipMm,
            TodayMin = temps.Min(),
            TodayMax = temps.Max(),
            Hours = strip,
            OutdoorHint = OutdoorHint(todayHours, today),
            ConditionSv = SymbolSv(nowHour.Symbol),
            YrUrl = string.IsNullOrWhiteSpace(yrUrl) ? YrPage(place) : yrUrl
        };
    }

    private static string OutdoorHint(List<WeatherHour> today, DateTime todayDate)
    {
        var work = today
            .Where(h => h.Local.Date == todayDate && h.Local.Hour is >= 8 and <= 18)
            .OrderBy(h => h.Local.Hour)
            .ToList();
        if (work.Count == 0)
            return "—";

        var sun = work.Where(h => IsSunny(h.Symbol)).Select(h => h.Local.Hour).Distinct().OrderBy(x => x).ToList();
        if (sun.Count > 0)
            return "Sol " + FormatRanges(sun);

        var part = work.Where(h => IsPartly(h.Symbol) && h.PrecipMm < 0.2)
            .Select(h => h.Local.Hour).Distinct().OrderBy(x => x).ToList();
        if (part.Count > 0)
            return "Delvis sol " + FormatRanges(part);

        if (work.Any(h => IsWet(h.Symbol) || h.PrecipMm >= 0.2))
            return "Regn/snö — dåligt ute";

        return "Mulet";
    }

    private static string FormatRanges(List<int> hours)
    {
        var parts = new List<string>();
        var start = hours[0];
        var prev = hours[0];
        for (var i = 1; i <= hours.Count; i++)
        {
            if (i < hours.Count && hours[i] == prev + 1)
            {
                prev = hours[i];
                continue;
            }
            parts.Add(start == prev ? $"{start:00}" : $"{start:00}–{prev:00}");
            if (i < hours.Count)
            {
                start = hours[i];
                prev = hours[i];
            }
        }
        return string.Join(", ", parts);
    }

    public static bool IsSunny(string symbol)
    {
        var s = BaseSymbol(symbol);
        return s is "clearsky" or "fair";
    }

    public static bool IsPartly(string symbol) => BaseSymbol(symbol) == "partlycloudy";

    public static bool IsWet(string symbol)
    {
        var s = BaseSymbol(symbol);
        return s.Contains("rain", StringComparison.Ordinal)
            || s.Contains("sleet", StringComparison.Ordinal)
            || s.Contains("snow", StringComparison.Ordinal)
            || s.Contains("thunder", StringComparison.Ordinal);
    }

    public static string BaseSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return "";
        return symbol
            .Replace("_day", "", StringComparison.OrdinalIgnoreCase)
            .Replace("_night", "", StringComparison.OrdinalIgnoreCase)
            .Replace("_polartwilight", "", StringComparison.OrdinalIgnoreCase);
    }

    public static string YrPage(string place)
    {
        if (place.Contains("Frö", StringComparison.OrdinalIgnoreCase) ||
            place.Contains("Fro", StringComparison.OrdinalIgnoreCase))
            return "https://www.yr.no/nb/v%C3%A6rvarsel/daglig-tabell/2-2673269/Sverige/Kalmar%20l%C3%A4n/M%C3%B6rbyl%C3%A5nga%20Kommun/Stora%20Fr%C3%B6";
        if (place.Contains("Kalmar", StringComparison.OrdinalIgnoreCase))
            return "https://www.yr.no/nb/v%C3%A6rvarsel/daglig-tabell/2-2702261/Sverige/Kalmar%20l%C3%A4n/Kalmar%20Municipality/Kalmar";
        if (place.Contains("Karleby", StringComparison.OrdinalIgnoreCase) ||
            place.Contains("Kokkola", StringComparison.OrdinalIgnoreCase))
            return "https://www.yr.no/nb/v%C3%A6rvarsel/daglig-tabell/2-651943/Finland/Mellersta%20%C3%96sterbotten/Kokkola/Karleby";
        return "https://www.yr.no/nb/search?q=" + Uri.EscapeDataString(place);
    }

    public static string SymbolSv(string symbol) => BaseSymbol(symbol) switch
    {
        "clearsky" => "Klarväder",
        "fair" => "Lettskyet",
        "partlycloudy" => "Delvis molnigt",
        "cloudy" => "Molnigt",
        "fog" => "Dimma",
        "lightrain" => "Lätt regn",
        "lightrainshowers" => "Lätta skurar",
        "rain" => "Regn",
        "rainshowers" => "Skurar",
        "heavyrain" => "Kraftigt regn",
        "heavyrainshowers" => "Kraftiga skurar",
        "lightsnow" => "Lätt snö",
        "snow" => "Snö",
        "heavysnow" => "Ymnig snö",
        "sleet" => "Snöblandat",
        "lightrainandthunder" => "Åska",
        "rainandthunder" => "Åska",
        "heavyrainandthunder" => "Åska",
        _ => string.IsNullOrWhiteSpace(symbol) ? "—" : "Väder"
    };

    private static TimeZoneInfo ResolveTz(string? id)
    {
        foreach (var candidate in new[] { id, "Europe/Stockholm", "W. Europe Standard Time", "Europe/Helsinki", "FLE Standard Time", "Europe/Tallinn" })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
            catch { /* try next */ }
        }
        return TimeZoneInfo.Local;
    }

    private static string ShortErr(Exception ex)
    {
        var m = ex.Message;
        if (m.Length > 48) m = m[..48] + "…";
        return m;
    }
}
