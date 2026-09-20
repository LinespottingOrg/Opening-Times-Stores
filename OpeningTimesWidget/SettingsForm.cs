// Settings — theme, sun place, xAI token (grey card UI).
using OpeningTimesWidget.Controls;
using OpeningTimesWidget.Models;
using OpeningTimesWidget.Services;
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget;

public sealed class SettingsForm : Form
{
    private readonly ThemePalette _p;
    private readonly TextBox _tokenBox;
    private readonly TextBox _placeBox;
    private readonly Label _placeStatus;
    private readonly ThemeToggle _themeToggle;
    private readonly CheckBox _showToken;
    private ThemeMode _theme;
    private double _lat;
    private double _lon;
    private string _tz;

    public ThemeMode SelectedTheme => _theme;
    public string? Token =>
        string.IsNullOrWhiteSpace(_tokenBox.Text) ? null : _tokenBox.Text.Trim();
    public string SunPlaceName => string.IsNullOrWhiteSpace(_placeBox.Text) ? "Stora Frö" : _placeBox.Text.Trim();
    public double SunLatitude => _lat;
    public double SunLongitude => _lon;
    public string SunTimezone => string.IsNullOrWhiteSpace(_tz) ? "Europe/Stockholm" : _tz;

    public SettingsForm(AppSettings settings, ThemePalette palette, ThemeMode theme)
    {
        _p = palette;
        _theme = theme;
        _lat = settings.SunLatitude != 0 ? settings.SunLatitude : 56.5708;
        _lon = settings.SunLongitude != 0 ? settings.SunLongitude : 16.4174;
        _tz = string.IsNullOrWhiteSpace(settings.SunTimezone) ? "Europe/Stockholm" : settings.SunTimezone;

        Text = "Settings";
        Icon = AppIcons.Load(32);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 470);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        BackColor = _p.WindowBg;
        ForeColor = _p.TextPrimary;
        Padding = new Padding(1);

        const int pad = 16;
        const int cardW = 388;

        var header = new Panel
        {
            Bounds = new Rectangle(1, 1, 418, 44),
            BackColor = _p.Header
        };
        var title = new Label
        {
            Text = "Settings",
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = _p.TextPrimary,
            BackColor = _p.Header,
            AutoSize = true,
            Location = new Point(14, 12)
        };
        var btnX = new Button
        {
            Text = "✕",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(36, 28),
            Location = new Point(374, 8),
            ForeColor = _p.TextSecondary,
            BackColor = _p.Header,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10f)
        };
        btnX.FlatAppearance.BorderSize = 0;
        btnX.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        header.Controls.Add(title);
        header.Controls.Add(btnX);

        var y = 56;
        Controls.Add(MakeSection("APPEARANCE", pad, y));
        y += 22;

        var themeCard = MakeCard(pad, y, cardW, 44);
        themeCard.Controls.Add(new Label
        {
            Text = "Theme",
            Font = new Font("Segoe UI", 10f),
            ForeColor = _p.TextPrimary,
            BackColor = _p.Surface,
            AutoSize = true,
            Location = new Point(12, 12)
        });
        _themeToggle = new ThemeToggle { Mode = theme, Location = new Point(cardW - 144, 7) };
        _themeToggle.ApplyPalette(_p);
        _themeToggle.ThemeChanged += (_, _) => _theme = _themeToggle.Mode;
        themeCard.Controls.Add(_themeToggle);
        Controls.Add(themeCard);
        y += 56;

        Controls.Add(MakeSection("SOL · PLATS (uppgång / nedgång)", pad, y));
        y += 22;

        var placeCard = MakeCard(pad, y, cardW, 96);
        placeCard.Controls.Add(new Label
        {
            Text = "Ort för sol (väder: Stora Frö + Kalmar)",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _p.TextSecondary,
            BackColor = _p.Surface,
            Location = new Point(12, 8),
            AutoSize = true
        });
        _placeBox = new TextBox
        {
            Location = new Point(12, 30),
            Size = new Size(cardW - 110, 28),
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _p.WindowBg,
            ForeColor = _p.TextPrimary,
            Text = string.IsNullOrWhiteSpace(settings.SunPlaceName) ? "Stora Frö" : settings.SunPlaceName
        };
        var btnResolve = MakeBtn("Hitta", false);
        btnResolve.Size = new Size(80, 28);
        btnResolve.Location = new Point(cardW - 92, 30);
        btnResolve.Click += async (_, _) => await ResolvePlaceAsync();
        _placeStatus = new Label
        {
            Text = $"{_lat:0.####}, {_lon:0.####} · {_tz}",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = _p.TextSecondary,
            BackColor = _p.Surface,
            Location = new Point(12, 66),
            Size = new Size(cardW - 24, 20),
            AutoEllipsis = true
        };
        placeCard.Controls.Add(_placeBox);
        placeCard.Controls.Add(btnResolve);
        placeCard.Controls.Add(_placeStatus);
        Controls.Add(placeCard);
        y += 108;

        Controls.Add(MakeSection("XAI API TOKEN", pad, y));
        y += 22;

        var tokenCard = MakeCard(pad, y, cardW, 108);
        tokenCard.Controls.Add(new Label
        {
            Text = "Weekly hours. Local only — never in source.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _p.TextSecondary,
            BackColor = _p.Surface,
            Location = new Point(12, 8),
            Size = new Size(cardW - 24, 18)
        });
        _tokenBox = new TextBox
        {
            Location = new Point(12, 32),
            Size = new Size(cardW - 24, 28),
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _p.WindowBg,
            ForeColor = _p.TextPrimary,
            PasswordChar = '•',
            Text = settings.XaiApiToken ?? ""
        };
        _showToken = new CheckBox
        {
            Text = "Show token",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _p.TextSecondary,
            BackColor = _p.Surface,
            AutoSize = true,
            Location = new Point(12, 68),
            FlatStyle = FlatStyle.Flat
        };
        _showToken.CheckedChanged += (_, _) =>
            _tokenBox.PasswordChar = _showToken.Checked ? '\0' : '•';
        tokenCard.Controls.Add(_tokenBox);
        tokenCard.Controls.Add(_showToken);
        Controls.Add(tokenCard);

        var footer = new Panel
        {
            Bounds = new Rectangle(1, 418, 418, 50),
            BackColor = _p.Header
        };
        var btnCancel = MakeBtn("Cancel", false);
        var btnSave = MakeBtn("Save", true);
        btnCancel.Location = new Point(200, 10);
        btnSave.Location = new Point(306, 10);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnSave.Click += async (_, _) =>
        {
            // Auto-resolve place if name changed without Hitta
            if (!string.Equals(_placeBox.Text.Trim(), settings.SunPlaceName, StringComparison.OrdinalIgnoreCase)
                || _lat == 0)
                await ResolvePlaceAsync();
            DialogResult = DialogResult.OK;
            Close();
        };
        footer.Controls.Add(btnCancel);
        footer.Controls.Add(btnSave);

        Controls.Add(header);
        Controls.Add(footer);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        Paint += (_, e) =>
        {
            using var pen = new Pen(_p.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        Point drag = default;
        void Wire(Control c)
        {
            c.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) drag = e.Location; };
            c.MouseMove += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Location = new Point(Location.X + e.X - drag.X, Location.Y + e.Y - drag.Y);
            };
        }
        Wire(header);
        Wire(title);
    }

    private async Task ResolvePlaceAsync()
    {
        var q = _placeBox.Text.Trim();
        if (q.Length == 0) q = "Stora Frö";
        _placeStatus.Text = "Söker plats…";
        _placeStatus.ForeColor = _p.TextSecondary;
        var geo = await SunTimesService.GeocodeAsync(q);
        if (geo == null)
        {
            _placeStatus.Text = "Hittades inte — behåller förra koordinaterna";
            _placeStatus.ForeColor = _p.Closed;
            return;
        }
        _lat = geo.Value.lat;
        _lon = geo.Value.lon;
        _placeBox.Text = geo.Value.name;
        if (!string.IsNullOrWhiteSpace(geo.Value.tz))
            _tz = geo.Value.tz!;
        _placeStatus.Text = $"{_lat:0.####}, {_lon:0.####} · {_tz}";
        _placeStatus.ForeColor = _p.AlertGreen;
    }

    private Label MakeSection(string text, int x, int y) =>
        new()
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 8f),
            ForeColor = _p.TextSecondary,
            BackColor = _p.WindowBg,
            AutoSize = true,
            Location = new Point(x + 2, y)
        };

    private Panel MakeCard(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Bounds = new Rectangle(x, y, w, h),
            BackColor = _p.Surface
        };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(_p.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    private Button MakeBtn(string text, bool primary)
    {
        var b = new Button
        {
            Text = text,
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f),
            Cursor = Cursors.Hand,
            BackColor = primary ? _p.AlertGreen : _p.Surface,
            ForeColor = primary ? Color.White : _p.TextPrimary
        };
        b.FlatAppearance.BorderColor = primary ? _p.AlertGreen : _p.Border;
        b.FlatAppearance.BorderSize = 1;
        return b;
    }
}
