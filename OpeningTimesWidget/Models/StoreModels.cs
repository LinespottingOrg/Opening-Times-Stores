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
    /// <summary>Primary place for sunrise + outdoor-work weather (Stora Frö).</summary>
    public string SunPlaceName { get; set; } = "Stora Frö";
    public double SunLatitude { get; set; } = 56.5708;
    public double SunLongitude { get; set; } = 16.4174;
    public string SunTimezone { get; set; } = "Europe/Stockholm";
    public List<WeatherPlace> WeatherPlaces { get; set; } = new();
    public List<StoreEntry> Stores { get; set; } = new();
}

/// <summary>A yr.no location shown in the weather bar.</summary>
public sealed class WeatherPlace
{
    public string Name { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Timezone { get; set; } = "Europe/Stockholm";
    public string YrUrl { get; set; } = "";
    public bool Primary { get; set; }

    public static IReadOnlyList<WeatherPlace> DavidDefaults { get; } =
    [
        new()
        {
            Name = "Stora Frö",
            Lat = 56.5708,
            Lon = 16.4174,
            Timezone = "Europe/Stockholm",
            YrUrl = "https://www.yr.no/nb/v%C3%A6rvarsel/daglig-tabell/2-2673269/Sverige/Kalmar%20l%C3%A4n/M%C3%B6rbyl%C3%A5nga%20Kommun/Stora%20Fr%C3%B6",
            Primary = true
        },
        new()
        {
            Name = "Kalmar",
            Lat = 56.6634,
            Lon = 16.3567,
            Timezone = "Europe/Stockholm",
            YrUrl = "https://www.yr.no/nb/v%C3%A6rvarsel/daglig-tabell/2-2702261/Sverige/Kalmar%20l%C3%A4n/Kalmar%20Municipality/Kalmar",
            Primary = false
        },
        new()
        {
            Name = "Karleby",
            Lat = 63.8385,
            Lon = 23.1307,
            Timezone = "Europe/Helsinki",
            YrUrl = "https://www.yr.no/nb/v%C3%A6rvarsel/daglig-tabell/2-651943/Finland/Mellersta%20%C3%96sterbotten/Kokkola/Karleby",
            Primary = false
        }
    ];
}
