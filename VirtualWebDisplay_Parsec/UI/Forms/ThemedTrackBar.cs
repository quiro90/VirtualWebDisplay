using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Forms;

internal sealed class ThemedTrackBar : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private int _tickFrequency = 10;
    private int _smallChange = 1;
    private int _largeChange = 10;
    private bool _dragging;
    private Color _trackColor = SystemColors.ControlDark;
    private Color _activeTrackColor = SystemColors.Highlight;
    private Color _thumbColor = SystemColors.Highlight;
    private Color _tickColor = SystemColors.ControlDarkDark;

    public ThemedTrackBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Height = 28;
        TabStop = true;
    }

    public event EventHandler? ValueChanged;

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
                _maximum = _minimum;
            Value = Math.Clamp(_value, _minimum, _maximum);
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_minimum > _maximum)
                _minimum = _maximum;
            Value = Math.Clamp(_value, _minimum, _maximum);
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Value
    {
        get => _value;
        set
        {
            var normalized = Math.Clamp(value, _minimum, _maximum);
            if (_value == normalized)
            {
                Invalidate();
                return;
            }

            _value = normalized;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int TickFrequency
    {
        get => _tickFrequency;
        set
        {
            _tickFrequency = Math.Max(1, value);
            Invalidate();
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int SmallChange
    {
        get => _smallChange;
        set => _smallChange = Math.Max(1, value);
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int LargeChange
    {
        get => _largeChange;
        set => _largeChange = Math.Max(1, value);
    }

    public void ApplyPalette(Color trackColor, Color activeTrackColor, Color thumbColor, Color tickColor)
    {
        _trackColor = trackColor;
        _activeTrackColor = activeTrackColor;
        _thumbColor = thumbColor;
        _tickColor = tickColor;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var trackRectangle = GetTrackRectangle();
        using var trackPen = new Pen(_trackColor, 4);
        using var activePen = new Pen(_activeTrackColor, 4);
        using var tickPen = new Pen(_tickColor, 1);
        using var thumbBrush = new SolidBrush(_thumbColor);

        e.Graphics.DrawLine(trackPen, trackRectangle.Left, trackRectangle.Top, trackRectangle.Right, trackRectangle.Top);

        var thumbCenterX = GetThumbCenterX(trackRectangle);
        e.Graphics.DrawLine(activePen, trackRectangle.Left, trackRectangle.Top, thumbCenterX, trackRectangle.Top);

        if (_tickFrequency > 0 && _maximum > _minimum)
        {
            for (var tick = _minimum; tick <= _maximum; tick += _tickFrequency)
            {
                var tickX = MapValueToX(trackRectangle, tick);
                e.Graphics.DrawLine(tickPen, tickX, trackRectangle.Top + 10, tickX, trackRectangle.Top + 14);
            }
        }

        e.Graphics.FillEllipse(thumbBrush, thumbCenterX - 7, trackRectangle.Top - 8, 14, 14);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        Focus();
        _dragging = true;
        SetValueFromMouse(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
            SetValueFromMouse(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Left)
            Value -= _smallChange;
        else if (e.KeyCode == Keys.Right)
            Value += _smallChange;
        else if (e.KeyCode == Keys.PageDown)
            Value -= _largeChange;
        else if (e.KeyCode == Keys.PageUp)
            Value += _largeChange;
        else if (e.KeyCode == Keys.Home)
            Value = _minimum;
        else if (e.KeyCode == Keys.End)
            Value = _maximum;
    }

    private Rectangle GetTrackRectangle()
    {
        return new Rectangle(8, Math.Max(Height / 2 - 1, 8), Math.Max(Width - 16, 10), 2);
    }

    private int GetThumbCenterX(Rectangle trackRectangle) => MapValueToX(trackRectangle, _value);

    private int MapValueToX(Rectangle trackRectangle, int value)
    {
        if (_maximum == _minimum)
            return trackRectangle.Left;

        var ratio = (double)(value - _minimum) / (_maximum - _minimum);
        return trackRectangle.Left + (int)Math.Round(trackRectangle.Width * ratio);
    }

    private void SetValueFromMouse(int x)
    {
        var trackRectangle = GetTrackRectangle();
        var clampedX = Math.Clamp(x, trackRectangle.Left, trackRectangle.Right);
        var ratio = trackRectangle.Width == 0 ? 0 : (double)(clampedX - trackRectangle.Left) / trackRectangle.Width;
        Value = _minimum + (int)Math.Round((_maximum - _minimum) * ratio);
    }
}