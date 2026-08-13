// Ranked store search — favorites first, alias-aware, clean scores for suggestions.
using System.Globalization;
using System.Text;
using OpeningTimesWidget.Models;

namespace OpeningTimesWidget.Services;

public sealed class SearchHit
{
    public required string DisplayName { get; init; }
    public string? Subtitle { get; init; }
    public int Score { get; init; }
    public bool IsFavorite { get; init; }
    public bool FromCatalog { get; init; }
    public StoreEntry? Store { get; init; }
    /// <summary>Matched alias or query fragment for UI hint.</summary>
    public string? MatchedOn { get; init; }
}

public static class StoreSearch
{
    /// <param name="preferFavorites">Search bar: boost list items. Quick-add: still show catalog.</param>
    public static List<SearchHit> Rank(string? query, IEnumerable<StoreEntry>? userStores = null,
        IEnumerable<string>? extraNames = null, int max = 10, bool preferFavorites = true)
    {
        var q = Normalize(query);
        if (q.Length == 0) return new List<SearchHit>();

        var hits = new Dictionary<string, SearchHit>(StringComparer.OrdinalIgnoreCase);
        var favNames = new HashSet<string>(
            (userStores ?? Enumerable.Empty<StoreEntry>())
            .Where(s => s.IsFavorite && !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);

        void Upsert(string name, int score, string? subtitle, bool fav, bool catalog,
            StoreEntry? store, string? matchedOn)
        {
            if (score <= 0 || string.IsNullOrWhiteSpace(name)) return;
            // Favorites always win a bit for search UX
            if (preferFavorites && (fav || favNames.Contains(name)))
                score += 15;

            if (!hits.TryGetValue(name, out var existing) || score > existing.Score)
            {
                hits[name] = new SearchHit
                {
                    DisplayName = name,
                    Subtitle = subtitle ?? existing?.Subtitle,
                    Score = score,
                    IsFavorite = fav || existing?.IsFavorite == true || favNames.Contains(name),
                    FromCatalog = catalog || existing?.FromCatalog == true,
                    Store = store ?? existing?.Store,
                    MatchedOn = matchedOn ?? existing?.MatchedOn
                };
            }
            else
            {
                hits[name] = new SearchHit
                {
                    DisplayName = existing.DisplayName,
                    Subtitle = MergeSub(existing.Subtitle, subtitle),
                    Score = existing.Score,
                    IsFavorite = existing.IsFavorite || fav || favNames.Contains(name),
                    FromCatalog = existing.FromCatalog || catalog,
                    Store = existing.Store ?? store,
                    MatchedOn = existing.MatchedOn ?? matchedOn
                };
            }
        }

        foreach (var c in StoreCatalog.All)
        {
            var best = ScoreName(q, c.CanonicalName);
            string? matched = best > 0 ? null : null;
            if (best > 0) matched = c.CanonicalName;

            foreach (var a in c.Aliases)
            {
                var sc = ScoreName(q, a);
                if (sc > best)
                {
                    best = sc;
                    matched = a;
                }
            }

            if (best <= 0) continue;

            var parts = new List<string> { c.Category };
            if (!string.IsNullOrEmpty(c.Place)) parts.Add(c.Place!);
            if (favNames.Contains(c.CanonicalName)) parts.Insert(0, "★ In list");
            else parts.Add("Catalog");

            Upsert(c.CanonicalName, best + 5, string.Join(" · ", parts),
                fav: favNames.Contains(c.CanonicalName), catalog: true, store: null,
                matchedOn: matched != null &&
                           !string.Equals(matched, c.CanonicalName, StringComparison.OrdinalIgnoreCase)
                    ? $"via “{matched}”"
                    : null);
        }

        if (userStores != null)
        {
            foreach (var s in userStores)
            {
                if (string.IsNullOrWhiteSpace(s.Name)) continue;
                var score = BestScoreForStore(q, s);
                if (score <= 0) continue;
                var cat = StoreCatalog.FindExact(s.Name);
                var sub = s.IsFavorite ? "★ Favorite" : "Saved";
                if (cat != null)
                    sub += " · " + cat.Category + (string.IsNullOrEmpty(cat.Place) ? "" : " · " + cat.Place);
                Upsert(s.Name, score + (s.IsFavorite ? 12 : 0), sub, s.IsFavorite, false, s, null);
            }
        }

        if (extraNames != null)
        {
            foreach (var n in extraNames)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                Upsert(n.Trim(), ScoreName(q, n), "Custom", false, false, null, null);
            }
        }

        return hits.Values
            .OrderByDescending(h => h.IsFavorite) // favorites first
            .ThenByDescending(h => h.Score)
            .ThenBy(h => h.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .ToList();
    }

    private static string? MergeSub(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a)) return b;
        if (string.IsNullOrEmpty(b)) return a;
        if (a.Contains(b, StringComparison.OrdinalIgnoreCase)) return a;
        if (b.Contains(a, StringComparison.OrdinalIgnoreCase)) return b;
        return a;
    }

    public static List<StoreEntry> FilterFavorites(IEnumerable<StoreEntry> stores, string? query)
    {
        var favs = stores.Where(s => s.IsFavorite).ToList();
        var q = Normalize(query);
        if (q.Length == 0) return favs;

        return favs
            .Select(s => new { Store = s, Score = BestScoreForStore(q, s) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Store.Name)
            .Select(x => x.Store)
            .ToList();
    }

    private static int BestScoreForStore(string qNorm, StoreEntry s)
    {
        var best = ScoreName(qNorm, s.Name);
        var cat = StoreCatalog.FindExact(s.Name);
        if (cat != null)
        {
            foreach (var a in cat.Aliases)
                best = Math.Max(best, ScoreName(qNorm, a));
            if (!string.IsNullOrEmpty(cat.Place))
                best = Math.Max(best, ScoreName(qNorm, cat.Place!) / 2);
        }
        if (!string.IsNullOrEmpty(s.Special))
            best = Math.Max(best, ScoreName(qNorm, s.Special!) / 2);
        foreach (var w in s.WatchedItems)
            if (!string.IsNullOrEmpty(w.Query))
                best = Math.Max(best, ScoreName(qNorm, w.Query) / 2);
        return best;
    }

    private static int ScoreName(string qNorm, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var n = Normalize(name);
        if (n.Length == 0) return 0;

        if (n == qNorm) return 100;
        if (n.StartsWith(qNorm, StringComparison.Ordinal)) return 90;
        if (n.Contains(" " + qNorm, StringComparison.Ordinal)) return 78;
        if (n.Contains(qNorm, StringComparison.Ordinal)) return 70;

        var words = n.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => w.StartsWith(qNorm, StringComparison.Ordinal))) return 82;

        var qWords = qNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (qWords.Length > 1 &&
            qWords.All(qw => words.Any(w =>
                w.StartsWith(qw, StringComparison.Ordinal) ||
                w.Contains(qw, StringComparison.Ordinal))))
            return 85;

        if (qNorm.Length >= 3)
        {
            var target = n.Length <= qNorm.Length + 4 ? n : n[..Math.Min(n.Length, qNorm.Length + 3)];
            var dist = Levenshtein(qNorm, target);
            if (dist <= 1) return 62;
            if (qNorm.Length >= 4 && dist <= 2) return 48;
        }

        return 0;
    }

    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = s.Trim().ToLowerInvariant()
            .Replace('å', 'a').Replace('ä', 'a').Replace('ö', 'o')
            .Replace('Å', 'a').Replace('Ä', 'a').Replace('Ö', 'o');
        var form = t.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-')
                sb.Append(ch == '-' ? ' ' : ch);
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int Levenshtein(string a, string b)
    {
        var n = a.Length;
        var m = b.Length;
        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;
        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }
        return d[n, m];
    }
}
