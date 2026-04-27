using System.Drawing;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Forms;

internal sealed class ThemedComboBox : ComboBox
{
    private Color _backgroundColor = SystemColors.Window;
    private Color _foregroundColor = SystemColors.WindowText;
    private Color _borderColor = SystemColors.ActiveBorder;
    private Color _selectionBackgroundColor = SystemColors.Highlight;
    private Color _selectionForegroundColor = SystemColors.HighlightText;

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
        Color selectionForegroundColor)
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _borderColor = borderColor;
        _selectionBackgroundColor = selectionBackgroundColor;
        _selectionForegroundColor = selectionForegroundColor;

        BackColor = backgroundColor;
        ForeColor = foregroundColor;
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0)
            return;

        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backgroundColor = Enabled
            ? (isSelected ? _selectionBackgroundColor : _backgroundColor)
            : SystemColors.Control;
        var foregroundColor = Enabled
            ? (isSelected ? _selectionForegroundColor : _foregroundColor)
            : SystemColors.GrayText;

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
}