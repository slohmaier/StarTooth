using System.Drawing.Drawing2D;

namespace StarTooth;

/// <summary>
/// The tray icon, drawn at runtime so the project stays free of binary assets: the Bluetooth rune
/// in white on a five-pointed star. The star is deliberately fat-waisted (a high inner radius) so
/// the glyph has room to breathe and still reads at 16px.
/// </summary>
internal static class TrayIcons
{
    private static readonly Color StarFill = Color.FromArgb(255, 0, 122, 255);
    private static readonly Color StarEdge = Color.FromArgb(255, 120, 190, 255);
    private static readonly Color GlyphColor = Color.White;

    /// <summary>
    /// The Bluetooth rune as a single polyline, in a normalised 0..1 box. Traced from the standard
    /// construction: cross-diagonal, down to the foot, straight up the stem, then the mirror.
    /// </summary>
    private static readonly PointF[] GlyphPath =
    [
        new(0.271f, 0.271f),
        new(0.729f, 0.729f),
        new(0.500f, 0.958f),
        new(0.500f, 0.042f),
        new(0.729f, 0.271f),
        new(0.271f, 0.729f),
    ];

    private static Icon? _star;

    internal static Icon Star => _star ??= Create();

    private static Icon Create()
    {
        // Render at exactly the size the shell asks for; letting Windows downscale a 32px bitmap
        // to the 16px tray slot visibly softens the rune.
        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
        using Bitmap bitmap = Render(size);

        // Icon.FromHandle does not take ownership, so materialise a standalone copy.
        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    /// <summary>Renders the artwork at an arbitrary size; geometry is scaled from the 32px design.</summary>
    internal static Bitmap Render(int size)
    {
        float s = size / 32f;
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        DrawStar(g, cx: 16f * s, cy: 16f * s, outer: 15.4f * s, inner: 9.6f * s, scale: s);
        // The glyph path only spans 0.271..0.729 horizontally but 0.042..0.958 vertically, so an
        // equal width/height here is what yields the rune's true 1:2 ink proportions.
        DrawGlyph(g, cx: 16f * s, cy: 15.2f * s, width: 20f * s, height: 20f * s, scale: s);
        return bitmap;
    }

    private static void DrawStar(Graphics g, float cx, float cy, float outer, float inner, float scale)
    {
        var points = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            double angle = (-Math.PI / 2) + (i * Math.PI / 5);
            float radius = i % 2 == 0 ? outer : inner;
            points[i] = new PointF(
                cx + (float)(Math.Cos(angle) * radius),
                cy + (float)(Math.Sin(angle) * radius));
        }

        using var brush = new SolidBrush(StarFill);
        using var pen = new Pen(StarEdge, 1.3f * scale) { LineJoin = LineJoin.Round };
        g.FillPolygon(brush, points);
        g.DrawPolygon(pen, points);
    }

    private static void DrawGlyph(Graphics g, float cx, float cy, float width, float height, float scale)
    {
        var points = new PointF[GlyphPath.Length];
        for (int i = 0; i < GlyphPath.Length; i++)
        {
            points[i] = new PointF(
                cx + ((GlyphPath[i].X - 0.5f) * width),
                cy + ((GlyphPath[i].Y - 0.5f) * height));
        }

        using var pen = new Pen(GlyphColor, 2.2f * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        g.DrawLines(pen, points);
    }
}
