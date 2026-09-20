// Parse "HH:mm–HH:mm" / "Closed" and status: closes-in, opens-in, opens tomorrow (yellow).
using System.Globalization;
using System.Text.RegularExpressions;
using OpeningTimesWidget.Models;

namespace OpeningTimesWidget.Services;

public enum OpenStatusKind
{
    None,
    OpenClosesIn,
    OpensLaterToday,
    ClosingNow,
    /// <summary>Closed now — show next open in soft yellow.</summary>
    OpensTomorrow,
    ClosedUnknown
}

/// <summary>Colour for “Closes in …” timer: green → yellow (≤45) → orange (≤40) → red (≤30).</summary>
public enum TimerUrgency
{
    None,
    Green,
    Yellow,
    Orange,
    Red,
    /// <summary>Opens tomorrow (soft yellow, not close-timer).</summary>
    OpensTomorrow
}

public readonly record struct OpenStatus(string Text, OpenStatusKind Kind, double? MinutesLeft = null)
{
    public bool UseSoftYellow => Kind is OpenStatusKind.OpensTomorrow;

    public TimerUrgency Urgency
    {
        get
        {
            if (Kind is OpenStatusKind.OpensTomorrow) return TimerUrgency.OpensTomorrow;
            if (Kind is OpenStatusKind.ClosingNow) return TimerUrgency.Red;
            if (Kind is not (OpenStatusKind.OpenClosesIn or OpenStatusKind.OpensLaterToday))
                return TimerUrgency.None;
            if (MinutesLeft is not double m) return TimerUrgency.Green;
            // Closing countdown only (not "opens in")
            if (Kind is OpenStatusKind.OpensLaterToday) return TimerUrgency.Green;
            if (m <= 30) return TimerUrgency.Red;
            if (m <= 40) return TimerUrgency.Orange;
            if (m <= 45) return TimerUrgency.Yellow;
            return TimerUrgency.Green;
        }
    }

    public static OpenStatus Empty => new("", OpenStatusKind.None);
}

public static class HoursHelper
{
    private static readonly Regex RangeRx = new(
        @"(\d{1,2})[:.](\d{2})\s*[–\-~—to]+\s*(\d{1,2})[:.](\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static OpenStatus GetStatus(StoreEntry store, int todayDow, DateTime now)
    {
        var todayText = GetDayHours(store, todayDow);

        if (string.IsNullOrWhiteSpace(todayText) || todayText is "—" or "-")
            return FindNextOpen(store, todayDow, now, forceTomorrow: true);

        if (todayText.Contains("Closed", StringComparison.OrdinalIgnoreCase))
            return FindNextOpen(store, todayDow, now, forceTomorrow: true);

        if (!TryParseRange(todayText, out var open, out var close))
            return FindNextOpen(store, todayDow, now, forceTomorrow: true);

        var openToday = now.Date + open;
        var closeToday = now.Date + close;
        if (close <= open)
            closeToday = closeToday.AddDays(1);

        if (now < openToday)
        {
            var untilOpen = openToday - now;
            return new OpenStatus("Opens in " + FormatSpan(untilOpen), OpenStatusKind.OpensLaterToday,
                untilOpen.TotalMinutes);
        }

        if (now >= closeToday)
            return FindNextOpen(store, todayDow, now, forceTomorrow: true);

        var left = closeToday - now;
        var mins = left.TotalMinutes;
        if (mins < 1)
            return new OpenStatus("Closing now", OpenStatusKind.ClosingNow, 0);

        return new OpenStatus("Closes in " + FormatSpan(left), OpenStatusKind.OpenClosesIn, mins);
    }

    /// <summary>Walk forward up to 7 days for the next opening time.</summary>
    private static OpenStatus FindNextOpen(StoreEntry store, int todayDow, DateTime now, bool forceTomorrow)
    {
        for (var add = 1; add <= 7; add++)
        {
            var dow = (todayDow + add) % 7;
            var text = GetDayHours(store, dow);
            if (string.IsNullOrWhiteSpace(text) ||
                text.Contains("Closed", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!TryParseRange(text, out var open, out _))
                continue;

            var openAt = now.Date.AddDays(add) + open;
            var time = openAt.ToString("HH:mm");

            if (add == 1)
                return new OpenStatus($"Opens tomorrow {time}", OpenStatusKind.OpensTomorrow);

            var dayName = openAt.ToString("ddd", CultureInfo.GetCultureInfo("en-GB"));
            return new OpenStatus($"Opens {dayName} {time}", OpenStatusKind.OpensTomorrow);
        }

        return new OpenStatus("Closed", OpenStatusKind.ClosedUnknown);
    }

    public static string GetDayHours(StoreEntry store, int dayOfWeek)
    {
        if (store.HoursByDay.TryGetValue(dayOfWeek, out var h) && !string.IsNullOrWhiteSpace(h))
            return h.Trim();
        var key = dayOfWeek.ToString();
        foreach (var kv in store.HoursByDay)
            if (kv.Key.ToString() == key && !string.IsNullOrWhiteSpace(kv.Value))
                return kv.Value.Trim();
        return "—";
    }

    /// <summary>Compact week in the widget, e.g. "mån–fre 09:00–19:00 · lör 09:00–17:00 · sön stängt".</summary>
    public static string FormatWeek(StoreEntry store)
    {
        int[] order = [1, 2, 3, 4, 5, 6, 0];
        string[] names = ["sön", "mån", "tis", "ons", "tor", "fre", "lör"];
        var days = order.Select(d =>
        {
            var raw = GetDayHours(store, d);
            if (string.IsNullOrWhiteSpace(raw) || raw is "—" or "-")
                return "stängt";
            if (raw.Contains("Closed", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("stäng", StringComparison.OrdinalIgnoreCase))
                return "stängt";
            return raw.Replace(" - ", "–").Replace("-", "–");
        }).ToArray();

        var parts = new List<string>();
        var i = 0;
        while (i < 7)
        {
            var j = i;
            while (j + 1 < 7 && days[j + 1] == days[i]) j++;
            var from = names[order[i]];
            var to = names[order[j]];
            var label = i == j ? from : $"{from}–{to}";
            parts.Add($"{label} {days[i]}");
            i = j + 1;
        }
        return string.Join("  ·  ", parts);
    }

    public static bool TryParseRange(string text, out TimeSpan open, out TimeSpan close)
    {
        open = default;
        close = default;
        var m = RangeRx.Match(text);
        if (!m.Success) return false;
        open = new TimeSpan(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), 0);
        close = new TimeSpan(int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
            int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture), 0);
        return true;
    }

    private static string FormatSpan(TimeSpan t)
    {
        if (t.TotalHours >= 1)
        {
            var h = (int)t.TotalHours;
            var m = t.Minutes;
            return m > 0 ? $"{h}h {m}m" : $"{h}h";
        }
        if (t.TotalMinutes >= 1)
            return $"{Math.Max(1, (int)Math.Ceiling(t.TotalMinutes))}m";
        return "<1m";
    }
}
