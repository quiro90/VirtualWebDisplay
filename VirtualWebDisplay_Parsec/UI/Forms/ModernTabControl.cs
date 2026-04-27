using System.Drawing;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Forms;

internal sealed class ModernTabControl : TabControl
{
    private Color _tabBackground = SystemColors.ControlDark;
    private Color _tabSelectedBackground = SystemColors.ControlLightLight;
    private Color _tabForeground = SystemColors.ControlText;
    private Color _tabBorder = SystemColors.ControlDark;
    private Color _pageBackground = SystemColors.Control;
    private Color _tabSelectedForeground = SystemColors.ControlText;

    public ModernTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        ItemSize = new Size(104, 28);
        SizeMode = TabSizeMode.Fixed;
        Padding = new Point(16, 6);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
    }

    public void ApplyPalette(
        Color tabBackground,
        Color tabSelectedBackground,
        Color tabForeground,
        Color tabSelectedForeground,
        Color tabBorder,
        Color pageBackground)
    {
        _tabBackground = tabBackground;
        _tabSelectedBackground = tabSelectedBackground;
        _tabForeground = tabForeground;
        _tabSelectedForeground = tabSelectedForeground;
        _tabBorder = tabBorder;
        _pageBackground = pageBackground;
        BackColor = pageBackground;

        foreach (TabPage page in TabPages)
        {
            page.BackColor = pageBackground;
            page.ForeColor = tabForeground;
            page.UseVisualStyleBackColor = false;
        }

        Invalidate();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);

        if (e.Control is TabPage page)
        {
            page.BackColor = _pageBackground;
            page.ForeColor = _tabForeground;
            page.UseVisualStyleBackColor = false;
        }
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= TabPages.Count)
            return;

        var tabPage = TabPages[e.Index];
        var tabBounds = Rectangle.Inflate(GetTabRect(e.Index), -1, 0);
        var isSelected = SelectedIndex == e.Index;
        var background = isSelected ? _tabSelectedBackground : _tabBackground;
        var foreground = isSelected ? _tabSelectedForeground : _tabForeground;

        using var backgroundBrush = new SolidBrush(background);
        using var borderPen = new Pen(_tabBorder);
        using var textBrush = new SolidBrush(foreground);
        using var textFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.FillRectangle(backgroundBrush, tabBounds);
        e.Graphics.DrawRectangle(borderPen, tabBounds);

        var textBounds = Rectangle.Inflate(tabBounds, -8, -4);
        e.Graphics.DrawString(tabPage.Text, Font, textBrush, textBounds, textFormat);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var backgroundBrush = new SolidBrush(_pageBackground);
        e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(_pageBackground);

        var headerBounds = new Rectangle(0, 0, Width, ItemSize.Height + 10);
        using var headerBrush = new SolidBrush(_pageBackground);
        e.Graphics.FillRectangle(headerBrush, headerBounds);

        for (var index = 0; index < TabCount; index++)
        {
            using var drawArgs = new DrawItemEventArgs(
                e.Graphics,
                Font,
                GetTabRect(index),
                index,
                index == SelectedIndex ? DrawItemState.Selected : DrawItemState.Default,
                _tabForeground,
                _pageBackground);

            OnDrawItem(drawArgs);
        }

        var pageBounds = DisplayRectangle;
        pageBounds.Inflate(1, 1);
        using var borderPen = new Pen(_tabBorder);
        e.Graphics.DrawRectangle(borderPen, pageBounds);
    }
}
