// Sunrise / sunset via Open-Meteo (no API key) + geocoding for place names.
using System.Globalization;
using System.Text.Json;

namespace OpeningTimesWidget.Services;

public sealed class SunDay
{
    public string Place { get; init; } = "Stora Frö";
    public TimeOnly? Sunrise { get; init; }
    public TimeOnly? Sunset { get; init; }
    public string? Error { get; init; }

    public string DisplayLine =>
        Error != null
            ? $"Sol · {Place} · {Error}"
            : $"Sol · {Place}  ·  ↑ {(Sunrise?.ToString("HH:mm") ?? "—")}  ·  ↓ {(Sunset?.ToString("HH:mm") ?? "—")}";
}

public static class SunTimesService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public static async Task<(double lat, double lon, string name, string? tz)?> GeocodeAsync(
        string place, CancellationToken ct = default)
    {
        try
        {
            var url =
                "https://geocoding-api.open-meteo.com/v1/search?name=" +
                Uri.EscapeDataString(place.Trim()) +
                "&count=5&language=sv&format=json";
            using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                return null;

            // Prefer Sweden if present
            JsonElement? pick = null;
            foreach (var r in results.EnumerateArray())
            {
                var country = r.TryGetProperty("country_code", out var cc) ? cc.GetString() : null;
                if (string.Equals(country, "SE", StringComparison.OrdinalIgnoreCase))
                {
                    pick = r;
                    break;
                }
            }
            pick ??= results[0];

            var el = pick.Value;
            var lat = el.GetProperty("latitude").GetDouble();
            var lon = el.GetProperty("longitude").GetDouble();
            var name = el.TryGetProperty("name", out var n) ? n.GetString() ?? place : place;
            var admin = el.TryGetProperty("admin1", out var a) ? a.GetString() : null;
            if (!string.IsNullOrWhiteSpace(admin) &&
                !name.Contains(admin, StringComparison.OrdinalIgnoreCase))
                name = $"{name}, {admin}";
            var tz = el.TryGetProperty("timezone", out var t) ? t.GetString() : null;
            return (lat, lon, name, tz);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<SunDay> GetTodayAsync(
        string place, double lat, double lon, string? timezone, CancellationToken ct = default)
    {
        try
        {
            var tz = string.IsNullOrWhiteSpace(timezone) ? "Europe/Stockholm" : timezone.Trim();
            var url =
                "https://api.open-meteo.com/v1/forecast?" +
                $"latitude={lat.ToString(CultureInfo.InvariantCulture)}" +
                $"&longitude={lon.ToString(CultureInfo.InvariantCulture)}" +
                "&daily=sunrise,sunset&timezone=" + Uri.EscapeDataString(tz) +
                "&forecast_days=1";

            using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return new SunDay { Place = place, Error = "sol-API fel" };

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var daily = doc.RootElement.GetProperty("daily");
            var riseStr = daily.GetProperty("sunrise")[0].GetString();
            var setStr = daily.GetProperty("sunset")[0].GetString();

            return new SunDay
            {
                Place = place,
                Sunrise = ParseLocalTime(riseStr),
                Sunset = ParseLocalTime(setStr)
            };
        }
        catch
        {
            return new SunDay { Place = place, Error = "offline" };
        }
    }

    private static TimeOnly? ParseLocalTime(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        // "2026-08-13T05:12" or with offset
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dt))
            return TimeOnly.FromDateTime(dt);
        return null;
    }
}
