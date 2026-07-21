using StarTooth.Bluetooth;

namespace StarTooth;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
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

        Application.Run(new TrayApplicationContext());
        return 0;
    }
}
