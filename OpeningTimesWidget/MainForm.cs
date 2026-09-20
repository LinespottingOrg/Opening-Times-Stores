// Widget chrome per Claude prototype: grip · title · Light/Dark · ⚙ · ⋯ · min · close
// Product-row styled search + quick-add; height grows so scrollbar rarely needed.
using OpeningTimesWidget.Controls;
using OpeningTimesWidget.Models;
using OpeningTimesWidget.Services;
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget;

public sealed class MainForm : Form
{
    // Chrome
    private readonly Panel _header = new() { Dock = DockStyle.Top, Height = 56 };
    private readonly Panel _body = new() { Dock = DockStyle.Fill };
    private readonly Panel _weatherHost = new() { Dock = DockStyle.Right, Width = WeatherBar.PanelWidth };
    private readonly WeatherBar _weatherBar = new() { Dock = DockStyle.Fill };
    private readonly Panel _dayStrip = new() { Dock = DockStyle.Top, Height = 32 };
    private readonly Panel _searchHost = new() { Dock = DockStyle.Top, Height = StoreRowControl.MinRowHeight + 16 };
    private readonly Panel _addHost = new() { Dock = DockStyle.Bottom, Height = StoreRowControl.MinRowHeight + 20 };
    // AutoScroll OFF by default — form grows instead. Only on if content > screen.
    private readonly Panel _listHost = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        // Generous bottom padding so last store never needs vertical scroll
        Padding = new Padding(14, 10, 14, 56)
    };

    private readonly Label _grip = new()
    {
        Text = "⠿",
        Font = new Font("Segoe UI Symbol", 14f),
        TextAlign = ContentAlignment.MiddleCenter,
        Size = new Size(28, 32),
        Cursor = Cursors.SizeAll
    };
    private readonly Label _title = new()
    {
        Text = "Opening Times EU",
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10.5f) // smaller app name
    };
    private readonly Label _dayLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI Semibold", 9.5f),
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(16, 0, 0, 0)
    };

    private readonly ThemeToggle _themeToggle = new();
    private readonly IconButton _btnSettings = new() { Glyph = "⚙", GlyphSize = 12f };
    private readonly IconButton _btnMenu = new() { Glyph = "⋯", GlyphSize = 14f };
    private readonly IconButton _btnMin = new() { Glyph = "–", GlyphSize = 14f };
    private readonly IconButton _btnClose = new() { Glyph = "✕", GlyphSize = 10f };

    private readonly PillField _search = new()
    {
        LeadingGlyph = "⌕",
        Placeholder = "Search stores…",
        ActionIsAdd = false
    };
    private readonly PillField _add = new()
    {
        LeadingGlyph = "＋",
        Placeholder = "Quick add store…",
        ActionIsAdd = true
    };

    private readonly SuggestionList _searchSuggestions = new();
    private readonly SuggestionList _addSuggestions = new();

    private readonly System.Windows.Forms.Timer _weekTimer = new() { Interval = 60 * 60 * 1000 };
    private readonly System.Windows.Forms.Timer _fitTimer = new() { Interval = 40 }; // debounce height fit
    private readonly System.Windows.Forms.Timer _closingTimer = new() { Interval = 60_000 }; // refresh "closes in"
    private readonly XaiUpdateService _xai = new();
    private AppSettings _settings = new();
    private ThemeMode _theme = ThemeMode.Dark;
    private ThemePalette _palette = AppTheme.Dark;
    private Point _drag;
    private NotifyIcon? _tray;
    private ContextMenuStrip? _ctx;
    private string _filter = "";
    private bool _fitting;
    private SunDay _sunDay = new();

    public MainForm()
    {
        Text = "Opening Times EU";
        Icon = AppIcons.Load(32);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(1180, 820);
        MinimumSize = new Size(1020, 520);
        Location = new Point(48, 30);
        DoubleBuffered = true;
        TopMost = false; // normal window priority — not always-on-top
        Padding = new Padding(1);

        BuildHeader();
        BuildWeatherSide();
        BuildDayStrip();
        BuildSearch();
        BuildAdd();
        BuildMenu();
        WireSuggestions();

        // Body: stores fill left, weather docked right.
        _weatherHost.Controls.Add(_weatherBar);
        _body.Controls.Add(_listHost);
        _body.Controls.Add(_weatherHost);

        // Dock: fill first, then top/bottom (WinForms docks last-added outer)
        Controls.Add(_body);
        Controls.Add(_searchHost);
        Controls.Add(_dayStrip);
        Controls.Add(_header);
        Controls.Add(_addHost);
        Controls.Add(_searchSuggestions);
        Controls.Add(_addSuggestions);

        _tray = new NotifyIcon
        {
            Text = "Opening Times EU",
            Visible = true,
            Icon = AppIcons.Load(16),
            ContextMenuStrip = _ctx
        };
        _tray.DoubleClick += (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); };

        Shown += (_, _) =>
        {
            if (Width < 1100) Width = 1100;
            LayoutWeather();
            LayoutStack(forScroll: false);
        };

        // Do NOT re-fit on every listHost.Resize (that fights AutoScroll and causes dual bars).
        Resize += (_, _) =>
        {
            if (_fitting) return;
            LayoutWeather();
            LayoutHeader();
            LayoutPills();
            PositionSuggestionPanels();
        };

        _fitTimer.Tick += (_, _) =>
        {
            _fitTimer.Stop();
            FitHeightToContent(force: true);
        };

        Load += async (_, _) =>
        {
            try
            {
                _settings = SettingsStore.Load();
                SettingsStore.EnsureFavorites(_settings);
                _theme = AppTheme.Parse(_settings.Theme);
                _themeToggle.Mode = _theme;
                ApplyTheme();
                LayoutWeather();
                RebuildList();
                BeginInvoke(() =>
                {
                    LayoutWeather();
                    FitHeightToContent(force: true);
                });
                _ = RefreshWeatherAsync();
                _weekTimer.Tick += async (_, _) => await RunUpdateAsync(false);
                _weekTimer.Start();
                _closingTimer.Tick += (_, _) =>
                {
                    // Refresh "Closes in …" lines without full reload
                    RebuildList(adjustWindowHeight: false);
                    _ = RefreshWeatherAsync();
                };
                _closingTimer.Start();
                await RunUpdateAsync(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Opening Times EU");
            }
        };

        Paint += (_, e) =>
        {
            using var pen = new Pen(_palette.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    // ── Header (Claude: ⠿ title Light/Dark ⚙ ⋯ – ✕) ─────────────────

    private void BuildHeader()
    {
        _header.Controls.Add(_grip);
        _header.Controls.Add(_title);
        _header.Controls.Add(_themeToggle);
        _header.Controls.Add(_btnSettings);
        _header.Controls.Add(_btnMenu);
        _header.Controls.Add(_btnMin);
        _header.Controls.Add(_btnClose);

        void WireDrag(Control c)
        {
            c.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) _drag = e.Location; };
            c.MouseMove += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Location = new Point(Location.X + e.X - _drag.X, Location.Y + e.Y - _drag.Y);
            };
        }
        WireDrag(_header);
        WireDrag(_grip);
        WireDrag(_title);

        _themeToggle.ThemeChanged += (_, _) =>
        {
            _theme = _themeToggle.Mode;
            _settings.Theme = AppTheme.ToSetting(_theme);
            SettingsStore.Save(_settings);
            ApplyTheme();
            RebuildList();
        };

        _btnSettings.Click += (_, _) => ShowSettings();
        _btnMenu.Click += (_, _) => _ctx?.Show(_btnMenu, new Point(0, _btnMenu.Height));
        _btnMin.Click += (_, _) => WindowState = FormWindowState.Minimized;
        _btnClose.Click += (_, _) =>
        {
            _tray?.Dispose();
            Application.Exit();
        };

        LayoutHeader();
    }

    private void LayoutHeader()
    {
        var y = (_header.Height - 32) / 2;
        _grip.Location = new Point(8, y);
        _title.Location = new Point(40, y + 4);

        // Right cluster: close, min, menu, settings, theme
        var x = ClientSize.Width - 12;
        void Place(Control c, int w = 32)
        {
            x -= w + 4;
            c.SetBounds(x, y, w, 32);
        }

        Place(_btnClose);
        Place(_btnMin);
        Place(_btnMenu);
        Place(_btnSettings);
        x -= _themeToggle.Width + 8;
        _themeToggle.Location = new Point(x, y + 1);

        // Title max width before theme control
        _title.MaximumSize = new Size(Math.Max(60, x - 48), 28);
    }

    private void BuildWeatherSide()
    {
        _weatherHost.Padding = new Padding(0);
        LayoutWeather();
    }

    /// <summary>Weather is ~40% of the window so DPI cannot clip it off the right edge.</summary>
    private void LayoutWeather()
    {
        var w = ClientSize.Width;
        if (w < 100) return;
        // Never narrower than 360, never more than half the window.
        var weatherW = Math.Clamp((int)(w * 0.42), 360, Math.Max(360, w / 2));
        if (w - weatherW < 320)
            weatherW = Math.Max(280, w - 320);
        _weatherHost.Width = weatherW;
        _weatherHost.MinimumSize = new Size(weatherW, 0);
    }

    private void BuildDayStrip()
    {
        _dayStrip.Controls.Add(_dayLabel);
        _dayLabel.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) _drag = e.Location; };
        _dayLabel.MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                Location = new Point(Location.X + e.X - _drag.X, Location.Y + e.Y - _drag.Y);
        };
    }

    private async Task RefreshWeatherAsync()
    {
        try
        {
            SettingsStore.EnsureWeatherPlaces(_settings);
            var primary = _settings.WeatherPlaces.FirstOrDefault(p => p.Primary) ?? _settings.WeatherPlaces[0];
            var sunTask = SunTimesService.GetTodayAsync(
                primary.Name, primary.Lat, primary.Lon, primary.Timezone);
            var wxTask = WeatherService.GetPlacesAsync(_settings.WeatherPlaces);
            _sunDay = await sunTask;
            var places = await wxTask;
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(() =>
                {
                    _weatherBar.Bind(places, _sunDay, _palette);
                    RebuildList(adjustWindowHeight: false);
                });
            }
        }
        catch
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(() =>
                    _weatherBar.Bind(WeatherToday.Empty("Stora Frö", "väder —"), _sunDay, _palette));
            }
        }
    }

    private void EnsureSunDefaults()
    {
        SettingsStore.EnsureWeatherPlaces(_settings);
    }

    private void BuildSearch()
    {
        _searchHost.Controls.Add(_search);
        _search.ActionClick += (_, _) =>
        {
            _search.Clear();
            _filter = "";
            _searchSuggestions.HideList();
            // Clear filter: refill list but keep window size stable
            RebuildList(adjustWindowHeight: false);
            _search.FocusBox();
        };
        _search.TextChanged += (_, _) =>
        {
            _filter = _search.Text ?? "";
            // NEVER resize window while typing search — that broke height + text
            RebuildList(adjustWindowHeight: false);
            UpdateSearchSuggestions();
        };
        _search.BoxKeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down && _searchSuggestions.HasItems) { e.Handled = true; _searchSuggestions.MoveSelection(1); }
            else if (e.KeyCode == Keys.Up && _searchSuggestions.HasItems) { e.Handled = true; _searchSuggestions.MoveSelection(-1); }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_searchSuggestions.SelectedHit is { } hit) ApplySearchHit(hit);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _search.Clear();
                _filter = "";
                _searchSuggestions.HideList();
                RebuildList(adjustWindowHeight: false);
            }
        };
        _searchHost.Resize += (_, _) => LayoutPills();
    }

    private void BuildAdd()
    {
        _addHost.Controls.Add(_add);
        _add.ActionClick += (_, _) => QuickAddStore();
        _add.TextChanged += (_, _) => UpdateAddSuggestions();
        _add.BoxKeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down && _addSuggestions.HasItems) { e.Handled = true; _addSuggestions.MoveSelection(1); }
            else if (e.KeyCode == Keys.Up && _addSuggestions.HasItems) { e.Handled = true; _addSuggestions.MoveSelection(-1); }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_addSuggestions.SelectedHit is { } hit)
                {
                    _add.Text = hit.DisplayName;
                    _addSuggestions.HideList();
                }
                QuickAddStore();
            }
            else if (e.KeyCode == Keys.Escape) _addSuggestions.HideList();
        };
        _addHost.Resize += (_, _) => LayoutPills();
    }

    private void LayoutPills()
    {
        const int side = 14;
        var h = StoreRowControl.MinRowHeight;
        _search.SetBounds(side, (_searchHost.Height - h) / 2, Math.Max(120, _searchHost.ClientSize.Width - side * 2), h);
        _add.SetBounds(side, (_addHost.Height - h) / 2, Math.Max(120, _addHost.ClientSize.Width - side * 2), h);
    }

    private void WireSuggestions()
    {
        _searchSuggestions.SuggestionChosen += (_, hit) => ApplySearchHit(hit);
        _addSuggestions.SuggestionChosen += (_, hit) =>
        {
            _add.Text = hit.DisplayName;
            _addSuggestions.HideList();
            QuickAddStore();
        };
    }

    private void BuildMenu()
    {
        _ctx = new ContextMenuStrip();
        _ctx.Items.Add("Refresh now", null, async (_, _) => await RunUpdateAsync(true));
        _ctx.Items.Add("Settings…", null, (_, _) => ShowSettings());
        _ctx.Items.Add("Reload settings", null, (_, _) =>
        {
            _settings = SettingsStore.Load();
            SettingsStore.EnsureFavorites(_settings);
            _theme = AppTheme.Parse(_settings.Theme);
            _themeToggle.Mode = _theme;
            ApplyTheme();
            RebuildList();
            FitHeightToContent(true);
            _ = RefreshWeatherAsync();
        });
        _ctx.Items.Add(new ToolStripSeparator());
        _ctx.Items.Add("Exit", null, (_, _) => { _tray?.Dispose(); Application.Exit(); });
        ContextMenuStrip = _ctx;
        _header.ContextMenuStrip = _ctx;
        _listHost.ContextMenuStrip = _ctx;
    }

    private void ApplyTheme()
    {
        _palette = AppTheme.For(_theme);
        BackColor = _palette.WindowBg;
        _header.BackColor = _palette.Header;
        _body.BackColor = _palette.WindowBg;
        _weatherHost.BackColor = _palette.Header;
        _dayStrip.BackColor = _palette.Header;
        _searchHost.BackColor = _palette.WindowBg;
        _addHost.BackColor = _palette.WindowBg;
        _listHost.BackColor = _palette.WindowBg;

        _grip.ForeColor = _palette.TextSecondary;
        _grip.BackColor = _palette.Header;
        _title.ForeColor = _palette.TextPrimary;
        _title.BackColor = _palette.Header;
        _weatherBar.BackColor = _palette.Header;
        _weatherBar.Bind(
            WeatherService.LatestAll.Count > 0
                ? WeatherService.LatestAll
                : new[] { WeatherService.Latest ?? WeatherToday.Empty("Stora Frö") },
            _sunDay, _palette);
        _dayLabel.ForeColor = _palette.DayStrip;
        _dayLabel.BackColor = _palette.Header;

        _themeToggle.ApplyPalette(_palette);
        _themeToggle.BackColor = _palette.Header;
        foreach (var b in new[] { _btnSettings, _btnMenu, _btnMin, _btnClose })
        {
            b.ApplyTheme(_palette);
            b.BackColor = _palette.Header;
        }

        _search.ApplyTheme(_palette);
        _add.ApplyTheme(_palette);
        _searchSuggestions.ApplyTheme(_palette);
        _addSuggestions.ApplyTheme(_palette);
        Invalidate();
    }

    // ── List + dynamic height ────────────────────────────────────────

    /// <param name="adjustWindowHeight">
    /// True only for load / add store / theme. False while searching so the window
    /// does not jump and crush text.
    /// </param>
    private void RebuildList(bool adjustWindowHeight = true)
    {
        _listHost.SuspendLayout();
        DisableListScroll();

        foreach (var c in _listHost.Controls.OfType<Control>().ToList())
        {
            _listHost.Controls.Remove(c);
            c.Dispose();
        }

        var day = (int)DateTime.Now.DayOfWeek;
        var favs = StoreSearch.FilterFavorites(_settings.Stores, _filter);
        // When not resizing, reserve scroll gutter so row width stays stable
        var w = RowWidth(forScroll: !adjustWindowHeight);
        var y = _listHost.Padding.Top;

        if (favs.Count == 0)
        {
            _listHost.Controls.Add(new Label
            {
                AutoSize = false,
                Size = new Size(w, 48),
                Location = new Point(_listHost.Padding.Left, y),
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = _palette.TextSecondary,
                Text = string.IsNullOrWhiteSpace(_filter)
                    ? "No favorites yet — search or quick-add below."
                    : $"No match for “{_filter.Trim()}”."
            });
        }
        else
        {
            foreach (var s in favs)
            {
                var row = new StoreRowControl
                {
                    Width = w,
                    Location = new Point(_listHost.Padding.Left, y)
                };
                row.Bind(s, day, _palette, WeatherService.Latest);
                row.Width = w;
                row.Relayout();
                _listHost.Controls.Add(row);
                y += row.Height + StoreRowControl.RowGap;
            }
        }

        _listHost.ResumeLayout(false);

        var totalFav = _settings.Stores.Count(x => x.IsFavorite);
        _dayLabel.Text = string.IsNullOrWhiteSpace(_filter)
            ? $"  {DateTime.Now:dddd · d MMM}   ·   {totalFav} favorites"
            : $"  {DateTime.Now:dddd · d MMM}   ·   {favs.Count} of {totalFav} match";

        LayoutPills();

        if (adjustWindowHeight)
        {
            FitHeightToContent(true);
        }
        else
        {
            // Keep current window size; only reflow rows inside
            LayoutStack(forScroll: true);
            var need = MeasureStackHeight();
            if (need > _listHost.ClientSize.Height + 4)
            {
                _listHost.AutoScroll = true;
                _listHost.AutoScrollMinSize = new Size(0, need);
                _listHost.HorizontalScroll.Enabled = false;
                _listHost.HorizontalScroll.Visible = false;
            }
            else
            {
                DisableListScroll();
            }
        }
    }

    /// <summary>Row width of the LEFT list only (not the weather column).</summary>
    private int RowWidth(bool forScroll)
    {
        var listInner = _listHost.ClientSize.Width;
        if (listInner < 80)
            listInner = Math.Max(320, ClientSize.Width - Padding.Horizontal - _weatherHost.Width);
        var scrollGutter = forScroll ? SystemInformation.VerticalScrollBarWidth + 2 : 0;
        return Math.Max(240, listInner - _listHost.Padding.Horizontal - scrollGutter);
    }

    private int MeasureStackHeight()
    {
        var h = _listHost.Padding.Top + _listHost.Padding.Bottom;
        var i = 0;
        var n = _listHost.Controls.Count;
        foreach (Control c in _listHost.Controls)
        {
            h += c.Height;
            // gap after every row except conceptually last still included for breathing room
            if (c is StoreRowControl)
                h += StoreRowControl.RowGap;
            i++;
        }
        if (i == 0) h += 48;
        // Extra air so content never sits flush against quick-add (kills V-scroll)
        return h + 40;
    }

    private void LayoutStack(bool forScroll)
    {
        var w = RowWidth(forScroll);
        var y = _listHost.Padding.Top;
        var x = _listHost.Padding.Left;
        foreach (Control c in _listHost.Controls)
        {
            c.MaximumSize = new Size(w, 4000);
            c.Width = w;
            c.Left = x;
            if (c is StoreRowControl row)
            {
                row.Width = w;
                row.Relayout();
            }
            c.Top = y;
            y += c.Height + (c is StoreRowControl ? StoreRowControl.RowGap : 0);
        }
    }

    private void DisableListScroll()
    {
        _listHost.AutoScroll = false;
        _listHost.AutoScrollMinSize = Size.Empty;
        _listHost.AutoScrollPosition = Point.Empty;
        // Force-hide both bars (WinForms can leave them sticky)
        try
        {
            _listHost.HorizontalScroll.Enabled = false;
            _listHost.HorizontalScroll.Visible = false;
            _listHost.HorizontalScroll.Maximum = 0;
            _listHost.VerticalScroll.Enabled = false;
            _listHost.VerticalScroll.Visible = false;
            _listHost.VerticalScroll.Maximum = 0;
        }
        catch { /* ignore */ }
    }

    private void ScheduleFit()
    {
        _fitTimer.Stop();
        _fitTimer.Start();
    }

    /// <summary>
    /// Grow the form tall enough for every store row — no vertical scrollbar when screen allows.
    /// </summary>
    private void FitHeightToContent(bool force)
    {
        if (_fitting) return;
        _fitting = true;
        try
        {
            DisableListScroll();
            LayoutStack(forScroll: false);

            var stackH = MeasureStackHeight();
            var chrome = _header.Height + _dayStrip.Height + _searchHost.Height + _addHost.Height;
            // Large slack: chrome + full stack + breathing room under last card
            var idealH = chrome + stackH + Padding.Vertical + 72;

            var wa = Screen.FromControl(this).WorkingArea;
            // Prefer almost full working height before introducing scroll
            var maxH = Math.Max(MinimumSize.Height, Math.Min((int)(wa.Height * 0.98), 1400));
            var minH = MinimumSize.Height;

            if (idealH <= maxH)
            {
                DisableListScroll();
                Height = Math.Clamp(idealH, minH, maxH);
                LayoutStack(forScroll: false);

                // Second pass: if list client still shorter than content, grow again
                var need = MeasureStackHeight();
                var have = _listHost.ClientSize.Height;
                if (need > have + 2 && Height < maxH)
                {
                    Height = Math.Min(maxH, Height + (need - have) + 32);
                    LayoutStack(forScroll: false);
                }

                DisableListScroll();
            }
            else
            {
                // Only when taller than screen — allow vertical scroll
                Height = maxH;
                LayoutStack(forScroll: true);
                _listHost.AutoScroll = true;
                _listHost.AutoScrollMinSize = new Size(0, MeasureStackHeight());
                _listHost.HorizontalScroll.Enabled = false;
                _listHost.HorizontalScroll.Visible = false;
                _listHost.VerticalScroll.Enabled = true;
            }

            if (Top + Height > wa.Bottom)
                Top = Math.Max(wa.Top + 4, wa.Bottom - Height - 8);
            if (Left < wa.Left) Left = wa.Left + 4;

            LayoutWeather();
            LayoutHeader();
            LayoutPills();
            PositionSuggestionPanels();
        }
        catch { }
        finally
        {
            _fitting = false;
        }
    }

    // ── Search / add ─────────────────────────────────────────────────

    private void ApplySearchHit(SearchHit hit)
    {
        _search.Text = hit.DisplayName;
        _filter = hit.DisplayName;
        _searchSuggestions.HideList();
        RebuildList(adjustWindowHeight: false);
        var inList = _settings.Stores.Any(s =>
            s.IsFavorite && string.Equals(s.Name, hit.DisplayName, StringComparison.OrdinalIgnoreCase));
        _dayLabel.Text = inList
            ? $"  Showing “{hit.DisplayName}”"
            : $"  “{hit.DisplayName}” — use quick-add + to pin";
    }

    private void UpdateSearchSuggestions()
    {
        var q = _search.Text?.Trim() ?? "";
        if (q.Length < 1) { _searchSuggestions.HideList(); return; }
        // Prefer favorites already on the widget
        var hits = StoreSearch.Rank(q, _settings.Stores, max: 8, preferFavorites: true);
        _searchSuggestions.ShowHits(hits, maxVisible: 6);
        PositionSuggestionPanels();
    }

    private void UpdateAddSuggestions()
    {
        var q = _add.Text?.Trim() ?? "";
        if (q.Length < 1) { _addSuggestions.HideList(); return; }
        // Catalog + list; still ranks well for new stores
        var hits = StoreSearch.Rank(q, _settings.Stores, max: 8, preferFavorites: false);
        _addSuggestions.ShowHits(hits, maxVisible: 6);
        PositionSuggestionPanels();
    }

    private void PositionSuggestionPanels()
    {
        // Align with search/add pills — no window resize
        if (_searchSuggestions.Visible)
        {
            var top = _header.Height + _dayStrip.Height + _searchHost.Height - 2;
            var w = Math.Max(220, ClientSize.Width - 28);
            _searchSuggestions.SetBounds(14, top, w, _searchSuggestions.Height);
            _searchSuggestions.BringToFront();
        }
        if (_addSuggestions.Visible)
        {
            var h = _addSuggestions.Height;
            var top = ClientSize.Height - _addHost.Height - h + 2;
            var w = Math.Max(220, ClientSize.Width - 28);
            _addSuggestions.SetBounds(14, Math.Max(80, top), w, h);
            _addSuggestions.BringToFront();
        }
    }

    private void QuickAddStore()
    {
        var typed = (_add.Text ?? "").Trim();
        if (string.IsNullOrEmpty(typed)) { _add.FocusBox(); return; }

        string name;
        if (_addSuggestions.Visible && _addSuggestions.SelectedHit is { } hit)
            name = hit.DisplayName;
        else
            name = StoreCatalog.ResolveCanonical(typed) ?? typed;

        var existing = _settings.Stores.FirstOrDefault(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null) existing.IsFavorite = true;
        else _settings.Stores.Add(SettingsStore.CreateDefaultHours(name));

        SettingsStore.Save(_settings);
        _add.Clear();
        _addSuggestions.HideList();
        _filter = "";
        _search.Clear();
        RebuildList();
        FitHeightToContent(true);
        _dayLabel.Text = $"  {DateTime.Now:dddd · d MMM}   ·   added “{name}”";
        _add.FocusBox();
    }

    private void ShowSettings()
    {
        using var dlg = new SettingsForm(_settings, _palette, _theme);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _settings.XaiApiToken = dlg.Token;
        _settings.Theme = AppTheme.ToSetting(dlg.SelectedTheme);
        _settings.SunPlaceName = dlg.SunPlaceName;
        _settings.SunLatitude = dlg.SunLatitude;
        _settings.SunLongitude = dlg.SunLongitude;
        _settings.SunTimezone = dlg.SunTimezone;
        SettingsStore.Save(_settings);

        _theme = dlg.SelectedTheme;
        _themeToggle.Mode = _theme;
        ApplyTheme();
        RebuildList();
        FitHeightToContent(true);
        _ = RefreshWeatherAsync();
        _dayLabel.Text = $"  {DateTime.Now:dddd · d MMM}   ·   settings saved";
    }

    private async Task RunUpdateAsync(bool force)
    {
        try
        {
            if (force) _settings.LastWeeklyUpdateUtc = null;
            var did = await _xai.TryWeeklyUpdateAsync(_settings);
            _settings = SettingsStore.Load();
            SettingsStore.EnsureFavorites(_settings);
            _theme = AppTheme.Parse(_settings.Theme);
            ApplyTheme();
            RebuildList();
            FitHeightToContent(true);
            _dayLabel.Text = did
                ? $"  {DateTime.Now:dddd · d MMM}   ·   updated from xAI"
                : $"  {DateTime.Now:dddd · d MMM}   ·   {_settings.Stores.Count(s => s.IsFavorite)} favorites";
        }
        catch
        {
            _dayLabel.Text = $"  {DateTime.Now:dddd · d MMM}   ·   update skipped";
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tray?.Dispose();
        base.Dispose(disposing);
    }
}
