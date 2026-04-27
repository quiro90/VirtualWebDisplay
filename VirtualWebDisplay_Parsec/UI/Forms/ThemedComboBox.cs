using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Forms;

internal sealed class ThemedComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;

    private Color _backgroundColor = SystemColors.Window;
    private Color _foregroundColor = SystemColors.WindowText;
    private Color _borderColor = SystemColors.ActiveBorder;
    private Color _selectionBackgroundColor = SystemColors.Highlight;
    private Color _selectionForegroundColor = SystemColors.HighlightText;
    private Color _buttonColor = SystemColors.Control;
    private Color _arrowColor = SystemColors.ControlText;

    public ThemedComboBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        IntegralHeight = false;
        ItemHeight = 20;
    }

    public void ApplyPalette(
        Color backgroundColor,
        Color foregroundColor,
        Color borderColor,
        Color selectionBackgroundColor,
        Color selectionForegroundColor,
        Color buttonColor,
        Color arrowColor)
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _borderColor = borderColor;
        _selectionBackgroundColor = selectionBackgroundColor;
        _selectionForegroundColor = selectionForegroundColor;
        _buttonColor = buttonColor;
        _arrowColor = arrowColor;

        BackColor = backgroundColor;
        ForeColor = foregroundColor;
        Invalidate();
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);

        if (message.Msg == WmPaint || message.Msg == WmNcPaint)
            PaintDropDownButtonOverlay();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0)
            return;

        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backgroundColor = Enabled && isSelected ? _selectionBackgroundColor : _backgroundColor;
        var foregroundColor = Enabled
            ? (isSelected ? _selectionForegroundColor : _foregroundColor)
            : DimColor(_foregroundColor);

        using var backgroundBrush = new SolidBrush(backgroundColor);
        using var foregroundBrush = new SolidBrush(foregroundColor);

        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

        var itemText = GetItemText(Items[e.Index]);
        var textBounds = Rectangle.Inflate(e.Bounds, -6, 0);
        TextRenderer.DrawText(
            e.Graphics,
            itemText,
            Font,
            textBounds,
            foregroundColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        using var borderPen = new Pen(_borderColor);
        var borderBounds = ClientRectangle;
        borderBounds.Width -= 1;
        borderBounds.Height -= 1;
        e.Graphics.DrawRectangle(borderPen, borderBounds);

        e.DrawFocusRectangle();
    }

    private void PaintDropDownButtonOverlay()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0)
            return;

        using var graphics = Graphics.FromHwnd(Handle);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var buttonWidth = SystemInformation.VerticalScrollBarWidth;
        var buttonRectangle = new Rectangle(
            Math.Max(1, ClientRectangle.Right - buttonWidth - 1),
            1,
            Math.Max(buttonWidth, 16),
            Math.Max(ClientRectangle.Height - 2, 1));

        using var buttonBrush = new SolidBrush(_buttonColor);
        using var borderPen = new Pen(_borderColor);
        graphics.FillRectangle(buttonBrush, buttonRectangle);
        graphics.DrawLine(borderPen, buttonRectangle.Left, buttonRectangle.Top, buttonRectangle.Left, buttonRectangle.Bottom);

        var centerX = buttonRectangle.Left + buttonRectangle.Width / 2;
        var centerY = buttonRectangle.Top + buttonRectangle.Height / 2;
        var arrowPoints = new[]
        {
            new Point(centerX - 4, centerY - 2),
            new Point(centerX + 4, centerY - 2),
            new Point(centerX, centerY + 2),
        };
        using var arrowBrush = new SolidBrush(Enabled ? _arrowColor : DimColor(_arrowColor));
        graphics.FillPolygon(arrowBrush, arrowPoints);

        var borderBounds = ClientRectangle;
        borderBounds.Width -= 1;
        borderBounds.Height -= 1;
        graphics.DrawRectangle(borderPen, borderBounds);
    }

    private static Color DimColor(Color color) =>
        Color.FromArgb(color.R / 2, color.G / 2, color.B / 2);
}