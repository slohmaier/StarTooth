using System.Globalization;

namespace StarTooth;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplyLanguageOverride(args);

        // Spike entry points, used while the connect path is being validated.
        if (args.Length > 0 && args[0] == "--list")
            return Spike.List();
        if (args.Length > 1 && args[0] == "--connect")
            return Spike.SetConnected(args[1], connect: true);
        if (args.Length > 1 && args[0] == "--disconnect")
            return Spike.SetConnected(args[1], connect: false);
        if (args.Length > 1 && args[0] == "--render-icon")
            return Spike.RenderIcon(args[1]);

        ApplicationConfiguration.Initialize();

        // Opts the standard controls into the Windows light/dark setting. Still experimental in
        // .NET 9, and it does not cover ToolStrip, which ThemedMenuRenderer handles instead.
#pragma warning disable WFO5001
        Application.SetColorMode(SystemColorMode.System);
#pragma warning restore WFO5001

        if (args.Length > 1 && args[0] == "--render-dialog")
            return Spike.RenderDialog(args[1]);
        if (args.Length > 1 && args[0] == "--render-menu")
            return Spike.RenderMenu(args[1]);

        Application.Run(new TrayApplicationContext());
        return 0;
    }

    /// <summary>
    /// Honours "--lang &lt;culture&gt;" anywhere in the arguments. The app otherwise follows the
    /// Windows display language; this exists so both translations can be checked on one machine.
    /// </summary>
    private static void ApplyLanguageOverride(string[] args)
    {
        int index = Array.IndexOf(args, "--lang");
        if (index < 0 || index + 1 >= args.Length)
            return;

        try
        {
            var culture = new CultureInfo(args[index + 1]);
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // An unknown culture simply leaves the system language in place.
        }
    }
}
