namespace StarTooth;

/// <summary>
/// Renders the tray menu in the system colours. WinForms menus are light-themed regardless of the
/// Windows setting, so the colour table and the text colour both have to be supplied by hand.
/// </summary>
internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
{
    internal ThemedMenuRenderer() : base(new ThemedColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        // Disabled entries are used as section labels and hints, so they must stay legible
        // rather than fading into the background.
        e.TextColor = e.Item?.Enabled == false ? Theme.DisabledForeground : Theme.Foreground;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Border);
        Rectangle bounds = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
    }

    private sealed class ThemedColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Background;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Theme.Highlight;
        public override Color MenuItemSelected => Theme.Highlight;
        public override Color MenuItemSelectedGradientBegin => Theme.Highlight;
        public override Color MenuItemSelectedGradientEnd => Theme.Highlight;
        public override Color MenuItemPressedGradientBegin => Theme.Highlight;
        public override Color MenuItemPressedGradientMiddle => Theme.Highlight;
        public override Color MenuItemPressedGradientEnd => Theme.Highlight;
        public override Color ImageMarginGradientBegin => Theme.Background;
        public override Color ImageMarginGradientMiddle => Theme.Background;
        public override Color ImageMarginGradientEnd => Theme.Background;
        public override Color SeparatorDark => Theme.Separator;
        public override Color SeparatorLight => Theme.Separator;
        public override Color CheckBackground => Theme.Highlight;
        public override Color CheckSelectedBackground => Theme.Highlight;
    }
}
