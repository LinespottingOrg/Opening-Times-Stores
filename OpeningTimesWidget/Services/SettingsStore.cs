// Persist settings under %LocalAppData%\OpeningTimesWidget\ — never store token in source.
using System.Text.Json;
using OpeningTimesWidget.Models;

namespace OpeningTimesWidget.Services;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Canonical favorites (Biltema listed once). Order preserved on seed/merge.</summary>
    public static readonly string[] FavoriteNames =
    {
        "KSRR Färjestaden",
        "KSRR Mörbylånga",
        "KSRR Kalmar",
        "Bauhaus",
        "Byggmax",
        "Biltema",
        "Jula",
        "Clas Ohlson"
    };

    public static string DataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpeningTimesWidget");

    public static string SettingsPath => Path.Combine(DataDir, "settings.json");

    public static AppSettings Load()
    {
        Directory.CreateDirectory(DataDir);
        if (!File.Exists(SettingsPath))
        {
            var seed = CreateSample();
            Save(seed);
            return seed;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? CreateSample();
            if (string.IsNullOrWhiteSpace(s.Theme)) s.Theme = "dark";
            EnsureWeatherPlaces(s);
            return s;
        }
        catch
        {
            return CreateSample();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOpts));
    }

    /// <summary>David's yr.no places: Stora Frö (ute) + Kalmar + Karleby.</summary>
    public static void EnsureWeatherPlaces(AppSettings settings)
    {
        var have = settings.WeatherPlaces ?? new List<WeatherPlace>();
        var need = have.Count == 0
            || have.Any(p => p.Name.Contains("Tallinn", StringComparison.OrdinalIgnoreCase))
            || !have.Any(p => p.Name.Equals("Karleby", StringComparison.OrdinalIgnoreCase))
            || string.Equals(settings.SunPlaceName, "Tallinn", StringComparison.OrdinalIgnoreCase);
        if (!need) return;

        settings.WeatherPlaces = WeatherPlace.DavidDefaults.Select(p => new WeatherPlace
        {
            Name = p.Name,
            Lat = p.Lat,
            Lon = p.Lon,
            Timezone = p.Timezone,
            YrUrl = p.YrUrl,
            Primary = p.Primary
        }).ToList();
        var primary = settings.WeatherPlaces.First(p => p.Primary);
        settings.SunPlaceName = primary.Name;
        settings.SunLatitude = primary.Lat;
        settings.SunLongitude = primary.Lon;
        settings.SunTimezone = primary.Timezone;
        Save(settings);
    }

    /// <summary>Add any missing favorites (does not remove user-added stores).</summary>
    public static void EnsureFavorites(AppSettings settings)
    {
        var changed = false;
        foreach (var name in FavoriteNames)
        {
            if (settings.Stores.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            settings.Stores.Add(DefaultStore(name));
            changed = true;
        }

        foreach (var s in settings.Stores)
        {
            if (FavoriteNames.Any(n => string.Equals(n, s.Name, StringComparison.OrdinalIgnoreCase))
                && !s.IsFavorite)
            {
                s.IsFavorite = true;
                changed = true;
            }

            // Fill missing place URLs from catalog
            if (string.IsNullOrWhiteSpace(s.Url))
            {
                var url = StoreCatalog.ResolveUrl(s.Name);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    s.Url = url;
                    changed = true;
                }
            }
        }

        if (changed) Save(settings);
    }

    public static AppSettings CreateSample()
    {
        var app = new AppSettings { XaiApiToken = null, LastWeeklyUpdateUtc = null, Theme = "dark" };
        foreach (var name in FavoriteNames)
            app.Stores.Add(DefaultStore(name));

        // Sample specials / green alert (demo until weekly xAI refresh)
        Set(app, "Bauhaus", "VVS 10%", watch: new WatchedItem { Query = "VVS pipe", OnSale = true, Note = "Sale" });
        Set(app, "Clas Ohlson", "Window sale 20%");
        Set(app, "Byggmax", "Timber 15%");
        Set(app, "Jula", null, mf: "09:00–20:00", sa: "09:00–18:00", su: "10:00–17:00");
        return app;
    }

    private static void Set(AppSettings app, string name, string? special,
        string? mf = null, string? sa = null, string? su = null, WatchedItem? watch = null)
    {
        var s = app.Stores.First(x => x.Name == name);
        if (special != null) s.Special = special;
        if (mf != null)
        {
            for (var d = 1; d <= 5; d++) s.HoursByDay[d] = mf;
        }
        if (sa != null) s.HoursByDay[6] = sa;
        if (su != null) s.HoursByDay[0] = su;
        if (watch != null) s.WatchedItems.Add(watch);
    }

    /// <summary>Public seed for quick-add / search resolve — known hours when catalog hits.</summary>
    public static StoreEntry CreateDefaultHours(string name) => DefaultStore(name);

    private static StoreEntry DefaultStore(string name)
    {
        // Sensible placeholders for Kalmar län retail / recycling — refined by weekly xAI
        var (mf, sa, su, special) = name switch
        {
            "KSRR Färjestaden" => ("07:00–18:00", "09:00–15:00", "Closed", (string?)null),
            "KSRR Mörbylånga" => ("07:00–18:00", "09:00–15:00", "Closed", null),
            "KSRR Kalmar" => ("07:00–19:00", "09:00–15:00", "Closed", null),
            "KSRR Borgholm" => ("07:00–18:00", "09:00–15:00", "Closed", null),
            "KSRR Nybro" => ("07:00–18:00", "09:00–15:00", "Closed", null),
            "Bauhaus" => ("08:00–20:00", "09:00–18:00", "10:00–16:00", "VVS 10%"),
            "Byggmax" => ("07:00–19:00", "08:00–16:00", "10:00–15:00", null),
            "Biltema" => ("09:00–20:00", "09:00–18:00", "10:00–17:00", null),
            "Jula" => ("09:00–20:00", "09:00–18:00", "10:00–17:00", null),
            "Clas Ohlson" => ("10:00–19:00", "10:00–17:00", "Closed", "Windows 20%"),
            "Rusta" => ("10:00–20:00", "10:00–18:00", "11:00–17:00", null),
            "IKEA Kalmar" => ("10:00–20:00", "10:00–20:00", "10:00–20:00", null),
            "Ikea" => ("10:00–20:00", "10:00–20:00", "10:00–20:00", null),
            "Elgiganten" => ("10:00–20:00", "10:00–18:00", "11:00–17:00", null),
            "ICA Maxi" => ("08:00–22:00", "08:00–22:00", "08:00–22:00", null),
            _ => ("09:00–18:00", "10:00–16:00", "Closed", null)
        };

        var s = new StoreEntry
        {
            Name = name,
            IsFavorite = true,
            Special = special,
            Url = StoreCatalog.ResolveUrl(name),
            HoursByDay =
            {
                [1] = mf, [2] = mf, [3] = mf, [4] = mf, [5] = mf,
                [6] = sa, [0] = su
            }
        };
        return s;
    }
}
