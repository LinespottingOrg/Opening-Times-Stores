// Pill search/add field — paint card in background only; never cover TextBox.
using OpeningTimesWidget.Theme;

namespace OpeningTimesWidget.Controls;

public sealed class PillField : Panel
{
    private readonly Label _leading = new()
    {
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI Symbol", 12f),
        AutoSize = false
    };
    private readonly TextBox _box = new()
    {
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 11f)
    };
    private readonly Label _action = new()
    {
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI Semibold", 14f),
        AutoSize = false,
        Cursor = Cursors.Hand
    };

    private ThemePalette _p = AppTheme.Dark;
    private bool _actionIsAdd;

    public event EventHandler? ActionClick;
    public new event EventHandler? TextChanged;
    public event KeyEventHandler? BoxKeyDown;

    public new string Text
    {
        get => _box.Text;
        set => _box.Text = value;
    }

    public string Placeholder
    {
        get => _box.PlaceholderText ?? "";
        set => _box.PlaceholderText = value;
    }

    public string LeadingGlyph
    {
        get => _leading.Text;
        set => _leading.Text = value;
    }

    public bool ActionIsAdd
    {
        get => _actionIsAdd;
        set
        {
            _actionIsAdd = value;
            _action.Text = value ? "+" : "✕";
            _action.Font = new Font("Segoe UI Semibold", value ? 16f : 10f);
            UpdateActionVisibility();
        }
    }

    public TextBox InnerBox => _box;

    public PillField()
    {
        Height = StoreRowControl.MinRowHeight;
        DoubleBuffered = true;
        Controls.Add(_leading);
        Controls.Add(_box);
        Controls.Add(_action);
        _box.TextChanged += (_, e) =>
        {
            UpdateActionVisibility();
            TextChanged?.Invoke(this, e);
        };
        _box.KeyDown += (_, e) => BoxKeyDown?.Invoke(this, e);
        _action.Click += (_, e) => ActionClick?.Invoke(this, e);
        _leading.Click += (_, _) => _box.Focus();
        Click += (_, _) => _box.Focus();
        Resize += (_, _) => LayoutInner();
        LayoutInner();
    }

    public void ApplyTheme(ThemePalette p)
    {
        _p = p;
        BackColor = p.Surface;
        _leading.ForeColor = p.TextSecondary;
        _leading.BackColor = p.Surface;
        _box.BackColor = p.Surface;
        _box.ForeColor = p.TextPrimary;
        _action.BackColor = p.Surface;
        UpdateActionVisibility();
        Invalidate();
    }

    public void FocusBox() => _box.Focus();
    public void Clear() => _box.Clear();

    private void UpdateActionVisibility()
    {
        if (_actionIsAdd)
        {
            _action.Visible = true;
            _action.ForeColor = _p.AlertGreen;
        }
        else
        {
            _action.Visible = !string.IsNullOrEmpty(_box.Text);
            _action.ForeColor = _p.TextSecondary;
        }
        LayoutInner();
    }

    private void LayoutInner()
    {
        const int pad = 10;
        var h = Height;
        _leading.SetBounds(pad, 0, 28, h);
        var actionW = _action.Visible ? 40 : 0;
        _action.SetBounds(Width - pad - actionW, 0, actionW, h);
        var boxLeft = pad + 28;
        var boxRight = Width - pad - actionW - 4;
        _box.SetBounds(boxLeft, (h - 22) / 2, Math.Max(40, boxRight - boxLeft), 22);
        _box.BringToFront();
        _action.BringToFront();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var parentBg = Parent?.BackColor ?? _p.WindowBg;
        e.Graphics.Clear(parentBg);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var b = new SolidBrush(_p.Surface);
        UiDraw.FillRound(e.Graphics, b, new Rectangle(0, 0, Width - 1, Height - 1), 10);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(_p.Border);
        UiDraw.DrawRound(e.Graphics, pen, new Rectangle(0, 0, Width - 1, Height - 1), 10);
    }
}
