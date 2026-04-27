using System.Drawing;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Theme;

/// <summary>
/// Renderer personalizado para ContextMenuStrip con soporte de tema dark/light.
/// </summary>
internal sealed class ThemedMenuRenderer(
    Color background,
    Color border,
    Color selectedBackground,
    Color foreground) : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds        = new Rectangle(Point.Empty, e.Item.Size);
        var itemBackColor = e.Item.Selected ? selectedBackground : background;
        using var brush   = new SolidBrush(itemBackColor);
        using var pen     = new Pen(border);
        e.Graphics.FillRectangle(brush, bounds);
        e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
        e.Item.ForeColor = foreground;
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(border);
        var bounds    = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, bounds);
    }
}
