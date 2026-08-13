// Weekly xAI chat completion to refresh hours/specials. Token from local settings only.
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OpeningTimesWidget.Models;

namespace OpeningTimesWidget.Services;

public sealed class XaiUpdateService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private const string Endpoint = "https://api.x.ai/v1/chat/completions";
    private const string Model = "grok-4.5"; // current xAI chat model (local token only)

    public async Task<bool> TryWeeklyUpdateAsync(AppSettings settings, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.XaiApiToken))
                return false; // no token: skip quietly

            if (settings.LastWeeklyUpdateUtc is DateTime last &&
                (DateTime.UtcNow - last) < TimeSpan.FromDays(7))
                return false; // not due yet

            var favs = settings.Stores.Where(s => s.IsFavorite).ToList();
            if (favs.Count == 0) return false;

            var prompt = BuildPrompt(favs);
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.XaiApiToken.Trim());
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = Model,
                temperature = 0.2,
                messages = new object[]
                {
                    new { role = "system", content = "Return ONLY valid JSON array of stores. No markdown." },
                    new { role = "user", content = prompt }
                }
            }), Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return false;

            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            ApplyResponse(settings, body);
            settings.LastWeeklyUpdateUtc = DateTime.UtcNow;
            SettingsStore.Save(settings);
            return true;
        }
        catch
        {
            // Never crash the widget on network/API errors
            return false;
        }
    }

    private static string BuildPrompt(List<StoreEntry> favs)
    {
        var names = string.Join(", ", favs.Select(f => f.Name));
        var watches = string.Join("; ",
            favs.SelectMany(f => f.WatchedItems.Select(w => $"{f.Name}:{w.Query}")));
        return
            $"For these Swedish retail stores: {names}. " +
            "Output JSON array: [{name, hours:{mon,tue,wed,thu,fri,sat,sun}, special, watches:[{query,onSale,priceDrop,note}]}]. " +
            $"Today-oriented hours (Europe/Stockholm). Watched: {watches}. special = short discount text or null.";
    }

    private static void ApplyResponse(AppSettings settings, string apiBody)
    {
        using var doc = JsonDocument.Parse(apiBody);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content)) return;

        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var i = content.IndexOf('\n');
            var j = content.LastIndexOf("```");
            if (i > 0 && j > i) content = content[(i + 1)..j].Trim();
        }

        using var arr = JsonDocument.Parse(content);
        foreach (var el in arr.RootElement.EnumerateArray())
        {
            var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;
            var store = settings.Stores.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (store == null) continue;

            if (el.TryGetProperty("special", out var sp) && sp.ValueKind == JsonValueKind.String)
                store.Special = sp.GetString();

            if (el.TryGetProperty("hours", out var hours) && hours.ValueKind == JsonValueKind.Object)
            {
                MapDay(store, hours, "sun", 0);
                MapDay(store, hours, "mon", 1);
                MapDay(store, hours, "tue", 2);
                MapDay(store, hours, "wed", 3);
                MapDay(store, hours, "thu", 4);
                MapDay(store, hours, "fri", 5);
                MapDay(store, hours, "sat", 6);
            }

            if (el.TryGetProperty("watches", out var watches) && watches.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in watches.EnumerateArray())
                {
                    var q = w.TryGetProperty("query", out var qe) ? qe.GetString() : null;
                    if (string.IsNullOrWhiteSpace(q)) continue;
                    var item = store.WatchedItems.FirstOrDefault(x =>
                        string.Equals(x.Query, q, StringComparison.OrdinalIgnoreCase));
                    if (item == null)
                    {
                        item = new WatchedItem { Query = q };
                        store.WatchedItems.Add(item);
                    }
                    if (w.TryGetProperty("onSale", out var os)) item.OnSale = os.ValueKind == JsonValueKind.True;
                    if (w.TryGetProperty("priceDrop", out var pd)) item.PriceDrop = pd.ValueKind == JsonValueKind.True;
                    if (w.TryGetProperty("note", out var note) && note.ValueKind == JsonValueKind.String)
                        item.Note = note.GetString();
                }
            }
        }
    }

    private static void MapDay(StoreEntry store, JsonElement hours, string key, int dow)
    {
        if (hours.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
            store.HoursByDay[dow] = v.GetString() ?? store.HoursByDay.GetValueOrDefault(dow, "—");
    }
}
