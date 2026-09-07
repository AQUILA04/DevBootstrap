using System.Drawing.Drawing2D;

namespace StackPilot;

internal enum SoftButtonKind
{
    Primary,
    Secondary,
    Ghost
}

/// <summary>Flat button with rounded corners and primary/secondary/ghost variants.</summary>
internal sealed class SoftButton : Button
{
    private SoftButtonKind _kind = SoftButtonKind.Secondary;
    private bool _hover;
    private bool _pressed;

    public SoftButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 9.25F);
        Size = new Size(120, 34);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        UpdateKindColors();
    }

    public SoftButtonKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            UpdateKindColors();
            Invalidate();
        }
    }

    public int CornerRadius { get; set; } = 10;

    private void UpdateKindColors()
    {
        switch (_kind)
        {
            case SoftButtonKind.Primary:
                BackColor = Color.FromArgb(0x2F, 0x9E, 0x7B);
                ForeColor = Color.White;
                break;
            case SoftButtonKind.Secondary:
                BackColor = Color.FromArgb(0xE8, 0xF5, 0xEF);
                ForeColor = Color.FromArgb(0x1F, 0x6B, 0x54);
                break;
            default:
                BackColor = Color.White;
                ForeColor = Color.FromArgb(0x13, 0x21, 0x1D);
                break;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.White);

        var fill = BackColor;
        var border = Color.FromArgb(0xD5, 0xE3, 0xDC);
        if (!Enabled)
        {
            fill = Color.FromArgb(0xEE, 0xF2, 0xF0);
            ForeColor = Color.FromArgb(0x8A, 0x9A, 0x94);
        }
        else if (_pressed)
        {
            fill = _kind switch
            {
                SoftButtonKind.Primary => Color.FromArgb(0x14, 0x30, 0x29),
                SoftButtonKind.Secondary => Color.FromArgb(0xCD, 0xE8, 0xDC),
                _ => Color.FromArgb(0xD5, 0xE3, 0xDC)
            };
        }
        else if (_hover)
        {
            fill = _kind switch
            {
                SoftButtonKind.Primary => Color.FromArgb(0x1F, 0x6B, 0x54),
                SoftButtonKind.Secondary => Color.FromArgb(0xD7, 0xEE, 0xE3),
                _ => Color.FromArgb(0xE8, 0xF5, 0xEF)
            };
        }

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, CornerRadius);
        using (var brush = new SolidBrush(fill))
        {
            e.Graphics.FillPath(brush, path);
        }

        if (_kind == SoftButtonKind.Ghost || (_kind == SoftButtonKind.Secondary && !_hover && !_pressed))
        {
            using var pen = new Pen(_kind == SoftButtonKind.Ghost ? border : Color.FromArgb(0xB7, 0xD6, 0xC8), 1.2f);
            e.Graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Host panel that draws a soft rounded border around a ComboBox.</summary>
internal sealed class SoftComboHost : Panel
{
    private readonly ComboBox _combo;

    public SoftComboHost(ComboBox combo)
    {
        _combo = combo;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(0xE8, 0xF5, 0xEF);
        Padding = new Padding(12, 6, 8, 6);
        Size = new Size(340, 40);

        combo.Dock = DockStyle.Fill;
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = Color.FromArgb(0xE8, 0xF5, 0xEF);
        combo.ForeColor = Color.FromArgb(0x13, 0x21, 0x1D);
        combo.Font = new Font("Segoe UI Semibold", 11F);
        combo.IntegralHeight = false;
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = 28;
        combo.Margin = new Padding(0);
        Controls.Add(combo);

        combo.DrawItem += OnDrawItem;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = SoftButtonPath(rect, 10);
        using (var brush = new SolidBrush(BackColor))
        {
            e.Graphics.FillPath(brush, path);
        }

        using var pen = new Pen(Color.FromArgb(0xB7, 0xD6, 0xC8), 1.4f);
        e.Graphics.DrawPath(pen, path);
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        e.DrawBackground();
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var back = selected ? Color.FromArgb(0x2F, 0x9E, 0x7B) : Color.FromArgb(0xE8, 0xF5, 0xEF);
        var fore = selected ? Color.White : Color.FromArgb(0x13, 0x21, 0x1D);
        using (var brush = new SolidBrush(back))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        var text = _combo.Items[e.Index]?.ToString() ?? "";
        TextRenderer.DrawText(
            e.Graphics,
            text,
            e.Font ?? Font,
            Rectangle.Inflate(e.Bounds, -8, 0),
            fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath SoftButtonPath(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
