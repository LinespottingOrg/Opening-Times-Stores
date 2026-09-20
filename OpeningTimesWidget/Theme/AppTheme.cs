// Named colors — Claude design handoff option 1c + light tokens from brief.
namespace OpeningTimesWidget.Theme;

public enum ThemeMode
{
    Dark,
    Light
}

public sealed class ThemePalette
{
    public required Color WindowBg { get; init; }
    public required Color Surface { get; init; }
    public required Color Header { get; init; }
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }
    public required Color Special { get; init; }
    public required Color SpecialBg { get; init; }
    public required Color AlertGreen { get; init; }
    public required Color AlertGreenBg { get; init; }
    public required Color Border { get; init; }
    public required Color Closed { get; init; }
    /// <summary>Soft yellow for “Opens tomorrow HH:mm” and closes-in ≤45m.</summary>
    public required Color OpensTomorrow { get; init; }
    /// <summary>Closes in ≤40 minutes.</summary>
    public required Color TimerOrange { get; init; }
    /// <summary>Closes in ≤30 minutes / closing now.</summary>
    public required Color TimerRed { get; init; }
    public required Color DayStrip { get; init; }
    public required Color Track { get; init; }
    public required Color Thumb { get; init; }
    /// <summary>Temperature ≥ 0 °C.</summary>
    public required Color TempPlus { get; init; }
    /// <summary>Temperature below 0 °C.</summary>
    public required Color TempMinus { get; init; }
}

public static class AppTheme
{
    public static ThemePalette Dark { get; } = new()
    {
        WindowBg = Color.FromArgb(0x1C, 0x1C, 0x20),
        Surface = Color.FromArgb(0x24, 0x24, 0x2A),
        Header = Color.FromArgb(0x28, 0x28, 0x30),
        TextPrimary = Color.FromArgb(0xF5, 0xF5, 0xF5),
        TextSecondary = Color.FromArgb(0xB4, 0xB4, 0xBE),
        Special = Color.FromArgb(0xFF, 0xC1, 0x07),
        SpecialBg = Color.FromArgb(0x3A, 0x32, 0x18),
        AlertGreen = Color.FromArgb(0x2E, 0xCC, 0x71),
        AlertGreenBg = Color.FromArgb(0x1A, 0x3A, 0x28),
        Border = Color.FromArgb(0x3A, 0x3A, 0x44),
        Closed = Color.FromArgb(0xCF, 0x7A, 0x6E),
        OpensTomorrow = Color.FromArgb(0xE8, 0xD4, 0x8B), // soft yellow
        TimerOrange = Color.FromArgb(0xFF, 0xA5, 0x40),
        TimerRed = Color.FromArgb(0xFF, 0x6B, 0x5A),
        DayStrip = Color.FromArgb(0x2E, 0xCC, 0x71),
        Track = Color.FromArgb(0x3A, 0x3A, 0x44),
        Thumb = Color.FromArgb(0xF5, 0xF5, 0xF5),
        TempPlus = Color.FromArgb(0x2E, 0xCC, 0x71),
        TempMinus = Color.FromArgb(0x5B, 0xA3, 0xFF)
    };

    public static ThemePalette Light { get; } = new()
    {
        WindowBg = Color.FromArgb(0xF4, 0xF5, 0xF7),
        Surface = Color.White,
        Header = Color.White,
        TextPrimary = Color.FromArgb(0x1A, 0x1A, 0x1E),
        TextSecondary = Color.FromArgb(0x5C, 0x5C, 0x66),
        Special = Color.FromArgb(0xC7, 0x7D, 0x00),
        SpecialBg = Color.FromArgb(0xFF, 0xF4, 0xD6),
        AlertGreen = Color.FromArgb(0x1B, 0x9E, 0x4B),
        AlertGreenBg = Color.FromArgb(0xE3, 0xF7, 0xEB),
        Border = Color.FromArgb(0xE2, 0xE3, 0xE8),
        Closed = Color.FromArgb(0xB5, 0x4A, 0x3C),
        OpensTomorrow = Color.FromArgb(0xC9, 0xA8, 0x2A), // soft gold on light
        TimerOrange = Color.FromArgb(0xE0, 0x7A, 0x00),
        TimerRed = Color.FromArgb(0xC6, 0x3A, 0x2B),
        DayStrip = Color.FromArgb(0x1B, 0x9E, 0x4B),
        Track = Color.FromArgb(0xE2, 0xE3, 0xE8),
        Thumb = Color.FromArgb(0x1A, 0x1A, 0x1E),
        TempPlus = Color.FromArgb(0x1B, 0x9E, 0x4B),
        TempMinus = Color.FromArgb(0x2B, 0x6C, 0xB0)
    };

    public static ThemePalette For(ThemeMode mode) => mode == ThemeMode.Light ? Light : Dark;

    public static ThemeMode Parse(string? s) =>
        string.Equals(s, "light", StringComparison.OrdinalIgnoreCase) ? ThemeMode.Light : ThemeMode.Dark;

    public static string ToSetting(ThemeMode mode) => mode == ThemeMode.Light ? "light" : "dark";
}
