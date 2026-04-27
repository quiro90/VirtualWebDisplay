using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Forms;

internal sealed class ThemedNumericUpDown : UserControl
{
    private readonly TextBox _textBox;
    private readonly Button _incrementButton;
    private readonly Button _decrementButton;

    private decimal _minimum;
    private decimal _maximum = 100;
    private decimal _increment = 1;
    private decimal _value;
    private int _decimalPlaces;
    private IReadOnlyList<decimal>? _allowedValues;
    private Color _backgroundColor = SystemColors.Window;
    private Color _foregroundColor = SystemColors.WindowText;
    private Color _borderColor = SystemColors.ActiveBorder;
    private Color _buttonColor = SystemColors.Control;
    private Color _buttonForegroundColor = SystemColors.ControlText;
    private bool _isInitialized;

    public ThemedNumericUpDown()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        Height = 24;

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Left = 6,
            Top = 5,
            Width = 42,
        };
        _textBox.Leave += (_, _) => CommitText();
        _textBox.KeyDown += TextBox_KeyDown;

        _incrementButton = CreateSpinButton();
        _incrementButton.Click += (_, _) => StepValue(_increment);

        _decrementButton = CreateSpinButton();
        _decrementButton.Click += (_, _) => StepValue(-_increment);

        Controls.AddRange([_textBox, _incrementButton, _decrementButton]);
        _isInitialized = true;
        UpdateLayout();
        UpdateText();
    }

    public event EventHandler? ValueChanged;

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public decimal Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
                _maximum = _minimum;
            Value = Clamp(_value);
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public decimal Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_minimum > _maximum)
                _minimum = _maximum;
            Value = Clamp(_value);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<decimal>? AllowedValues
    {
        get => _allowedValues;
        set
        {
            _allowedValues = value is { Count: > 0 } ? value : null;
            if (_allowedValues is not null)
            {
                _minimum = _allowedValues[0];
                _maximum = _allowedValues[^1];
                Value = SnapToAllowed(_value);
            }
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public decimal Increment
    {
        get => _increment;
        set => _increment = value <= 0 ? 1 : value;
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            _decimalPlaces = Math.Max(0, value);
            UpdateText();
        }
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public decimal Value
    {
        get => _value;
        set
        {
            var normalized = Clamp(decimal.Round(value, _decimalPlaces, MidpointRounding.AwayFromZero));
            if (_value == normalized)
            {
                UpdateText();
                return;
            }

            _value = normalized;
            UpdateText();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public override Color BackColor
    {
        get => base.BackColor;
        set
        {
            base.BackColor = value;
            Invalidate();
        }
    }

    public void ApplyPalette(Color backgroundColor, Color foregroundColor, Color borderColor, Color buttonColor, Color buttonForegroundColor)
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _borderColor = borderColor;
        _buttonColor = buttonColor;
        _buttonForegroundColor = buttonForegroundColor;

        _textBox.BackColor = backgroundColor;
        _textBox.ForeColor = foregroundColor;
        _incrementButton.BackColor = buttonColor;
        _incrementButton.ForeColor = buttonForegroundColor;
        _decrementButton.BackColor = buttonColor;
        _decrementButton.ForeColor = buttonForegroundColor;
        UpdateLayout();
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _textBox.Enabled = Enabled;
        _incrementButton.Enabled = Enabled;
        _decrementButton.Enabled = Enabled;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateLayout();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var borderPen = new Pen(_borderColor);
        var borderBounds = ClientRectangle;
        borderBounds.Width -= 1;
        borderBounds.Height -= 1;
        e.Graphics.DrawRectangle(borderPen, borderBounds);

        // Vertical separator between text area and spin buttons
        var buttonLeft = Math.Max(Width - 17, 0);
        e.Graphics.DrawLine(borderPen, buttonLeft, 0, buttonLeft, Height - 1);

        // Draw spin button labels directly (WinForms flat buttons inside AllPaintingInWmPaint
        // UserControls do not reliably render their own text)
        var symbolColor = Enabled ? _buttonForegroundColor : SystemColors.GrayText;
        using var symbolFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        var halfH = Math.Max(Height / 2, 10);
        var incBounds = new Rectangle(buttonLeft, 0, 17, halfH);
        var decBounds = new Rectangle(buttonLeft, halfH - 1, 17, Height - halfH + 1);
        TextRenderer.DrawText(e.Graphics, "▲", symbolFont, incBounds, symbolColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        TextRenderer.DrawText(e.Graphics, "▼", symbolFont, decBounds, symbolColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private Button CreateSpinButton()
    {
        var btn = new Button
        {
            Width = 16,
            Height = 11,
            FlatStyle = FlatStyle.Flat,
            TabStop = false,
            Text = string.Empty,
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void UpdateLayout()
    {
        if (!_isInitialized)
            return;

        var buttonLeft = Math.Max(Width - 17, 0);
        _textBox.SetBounds(6, 5, Math.Max(buttonLeft - 8, 20), Math.Max(Height - 10, 12));
        _incrementButton.SetBounds(buttonLeft, 0, 17, Math.Max(Height / 2, 10));
        _decrementButton.SetBounds(buttonLeft, Math.Max(Height / 2 - 1, 0), 17, Math.Max(Height - (Height / 2) + 1, 10));
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            CommitText();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Up)
        {
            StepValue(_increment);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Down)
        {
            StepValue(-_increment);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void CommitText()
    {
        if (decimal.TryParse(_textBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(_textBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            Value = _allowedValues is not null ? SnapToAllowed(parsed) : parsed;
            return;
        }

        UpdateText();
    }

    private void StepValue(decimal delta)
    {
        if (_allowedValues is not null)
        {
            if (delta > 0)
            {
                var next = _allowedValues.FirstOrDefault(v => v > _value);
                Value = next == default && !_allowedValues.Contains(_value) ? _allowedValues[^1] : (next == default ? _value : next);
            }
            else
            {
                var prev = _allowedValues.LastOrDefault(v => v < _value);
                Value = prev == default && !_allowedValues.Contains(_value) ? _allowedValues[0] : (prev == default ? _value : prev);
            }
            return;
        }

        Value += delta;
    }

    private void UpdateText()
    {
        _textBox.Text = _value.ToString($"F{_decimalPlaces}", CultureInfo.CurrentCulture);
    }

    private decimal Clamp(decimal value) => Math.Min(_maximum, Math.Max(_minimum, value));

    private decimal SnapToAllowed(decimal value)
    {
        if (_allowedValues is null || _allowedValues.Count == 0)
            return Clamp(value);

        return _allowedValues.MinBy(v => Math.Abs(v - value));
    }
}