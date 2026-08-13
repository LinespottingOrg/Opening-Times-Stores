// Known stores + aliases + place URLs so names resolve and open correctly.
namespace OpeningTimesWidget.Services;

public sealed class CatalogEntry
{
    public required string CanonicalName { get; init; }
    public string Category { get; init; } = "Retail";
    public string? Place { get; init; }
    /// <summary>Official store page or Google Maps place link.</summary>
    public string? Url { get; init; }
    public string[] Aliases { get; init; } = Array.Empty<string>();
}

public static class StoreCatalog
{
    public static IReadOnlyList<CatalogEntry> All { get; } = new List<CatalogEntry>
    {
        // KSRR recycling — maps to each site
        E("KSRR Färjestaden", "Recycling", "Öland",
            Maps("KSRR Färjestaden återvinningscentral"),
            "ksrr farjestaden", "ksrr färjestaden", "farjestaden", "färjestaden", "åvc färjestaden"),
        E("KSRR Mörbylånga", "Recycling", "Öland",
            Maps("KSRR Mörbylånga återvinningscentral"),
            "ksrr morbylanga", "ksrr mörbylånga", "mörbylånga", "morbylanga", "åvc mörbylånga"),
        E("KSRR Kalmar", "Recycling", "Kalmar",
            "https://www.ksrr.se/",
            "ksrr", "åvc kalmar", "ksrr kalmar kommun"),
        E("KSRR Borgholm", "Recycling", "Öland",
            Maps("KSRR Borgholm återvinningscentral"),
            "borgholm åvc"),
        E("KSRR Nybro", "Recycling", "Nybro",
            Maps("KSRR Nybro återvinningscentral"),
            "åvc nybro"),

        E("Bauhaus", "DIY", "Kalmar",
            "https://www.bauhaus.se/varuhus/kalmar",
            "bau haus", "bauhouse"),
        E("Byggmax", "DIY", "Kalmar",
            "https://www.byggmax.se/vara-varuhus/kalmar",
            "bygg max", "byggmax kalmar"),
        E("Biltema", "Retail", "Kalmar",
            "https://www.biltema.se/hitta-varuhus/kalmar/",
            "bil tema"),
        E("Jula", "Retail", "Kalmar",
            "https://www.jula.se/storefinder/store/jula-kalmar/",
            "jula kalmar"),
        E("Clas Ohlson", "Retail", "Kalmar",
            "https://www.clasohlson.com/se/stores",
            "clasohlson", "clas olson", "class ohlson"),
        E("Rusta", "Retail", null, "https://www.rusta.com/se/", "rusta"),
        E("ÖoB", "Retail", null, "https://www.oob.se/", "oob", "öob"),
        E("IKEA Kalmar", "Retail", "Kalmar",
            "https://www.ikea.com/se/sv/stores/kalmar/",
            "ikea", "ikea kalmar", "ikea kalmar småland"),
        E("Hornbach", "DIY", null, "https://www.hornbach.se/", "hornbach"),
        E("XL-Bygg", "DIY", null, "https://www.xlbygg.se/", "xl bygg", "xlbygg"),
        E("Beijer Byggmaterial", "DIY", null, "https://www.beijerbygg.se/", "beijer", "beijer bygg"),
        E("K-Rauta", "DIY", null, "https://www.k-rauta.se/", "krauta", "k rauta"),

        E("OKQ8", "Fuel", null, "https://www.okq8.se/", "ok q8", "okq8"),
        E("Circle K", "Fuel", null, "https://www.circlek.se/", "circlek"),
        E("Intersport", "Sport", null, "https://www.intersport.se/", "intersport"),
        E("Stadium", "Sport", null, "https://www.stadium.se/", "stadium"),
        E("XXL", "Sport", null, "https://www.xxl.se/", "xxl"),
        E("Elgiganten", "Electronics", "Kalmar",
            "https://www.elgiganten.se/store/kalmar",
            "el giganten"),
        E("MediaMarkt", "Electronics", null, "https://www.mediamarkt.se/", "media markt", "mediamarkt"),
        E("Power", "Electronics", null, "https://www.power.se/", "power"),
        E("NetOnNet", "Electronics", null, "https://www.netonnet.se/", "net on net"),
        E("ICA Maxi", "Grocery", "Kalmar",
            Maps("ICA Maxi Kalmar"),
            "ica", "ica maxi kalmar"),
        E("Willys", "Grocery", null, "https://www.willys.se/", "willys"),
        E("Coop", "Grocery", null, "https://www.coop.se/", "coop forum"),
        E("Lidl", "Grocery", null, "https://www.lidl.se/", "lidl"),
    };

    private static string Maps(string query) =>
        "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query);

    private static CatalogEntry E(string name, string cat, string? place, string? url, params string[] aliases) =>
        new() { CanonicalName = name, Category = cat, Place = place, Url = url, Aliases = aliases };

    public static CatalogEntry? FindExact(string name) =>
        All.FirstOrDefault(c =>
            string.Equals(c.CanonicalName, name, StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)));

    public static string? ResolveCanonical(string query)
    {
        var hits = StoreSearch.Rank(query, extraNames: Array.Empty<string>());
        if (hits.Count == 0) return null;
        return hits[0].Score >= 40 ? hits[0].DisplayName : null;
    }

    public static string? ResolveUrl(string storeName)
    {
        var c = FindExact(storeName);
        if (!string.IsNullOrWhiteSpace(c?.Url)) return c!.Url;
        // Fallback: Google Maps search for the place
        return Maps(storeName + " Kalmar");
    }
}
