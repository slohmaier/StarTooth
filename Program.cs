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
        Application.Run(new TrayApplicationContext());
        return 0;
    }
}
