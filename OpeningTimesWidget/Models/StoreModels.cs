// Domain models for favorites, hours, specials, and watched items.
namespace OpeningTimesWidget.Models;

/// <summary>One favorited store row on the widget.</summary>
public sealed class StoreEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public bool IsFavorite { get; set; } = true;
    /// <summary>Store / place URL — click name to open in browser.</summary>
    public string? Url { get; set; }
    /// <summary>Key = DayOfWeek (0=Sunday..6=Saturday), value e.g. "09:00–18:00" or "Closed".</summary>
    public Dictionary<int, string> HoursByDay { get; set; } = new();
    /// <summary>Discount blurb shown to the right of the store name (e.g. "VVS 10%").</summary>
    public string? Special { get; set; }
    /// <summary>Items the user watches for sale / price-drop alerts.</summary>
    public List<WatchedItem> WatchedItems { get; set; } = new();
}

/// <summary>Search/watch term; green alert when OnSale or PriceDrop is true.</summary>
public sealed class WatchedItem
{
    public string Query { get; set; } = "";
    public bool OnSale { get; set; }
    public bool PriceDrop { get; set; }
    public string? Note { get; set; }
}

/// <summary>Root settings file (LocalAppData). Token never in source.</summary>
public sealed class AppSettings
{
    public string? XaiApiToken { get; set; }
    public DateTime? LastWeeklyUpdateUtc { get; set; }
    /// <summary>"dark" or "light" — persisted theme for dark/light slider.</summary>
    public string Theme { get; set; } = "dark";
    /// <summary>Place name for sunrise/sunset (default Kalmar).</summary>
    public string SunPlaceName { get; set; } = "Kalmar";
    public double SunLatitude { get; set; } = 56.6634;
    public double SunLongitude { get; set; } = 16.3567;
    public string SunTimezone { get; set; } = "Europe/Stockholm";
    public List<StoreEntry> Stores { get; set; } = new();
}
