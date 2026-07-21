namespace StarTooth;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Settings settings = Settings.Load();
        AppSetup.ApplyLanguage(settings);

        // An explicit --lang wins over the stored preference, so both translations can be
        // inspected without changing the user's settings.
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

        // Covers the standard controls; ToolStrip is left to ThemedMenuRenderer.
        AppSetup.ApplyColorMode(settings);

        if (args.Length > 1 && args[0] == "--render-dialog")
            return Spike.RenderDialog(args[1]);
        if (args.Length > 1 && args[0] == "--render-menu")
            return Spike.RenderMenu(args[1]);
        if (args.Length > 1 && args[0] == "--render-settings")
            return Spike.RenderSettings(args[1], settings);

        Application.Run(new TrayApplicationContext(settings));
        return 0;
    }

    /// <summary>Honours "--lang &lt;culture&gt;" anywhere in the arguments.</summary>
    private static void ApplyLanguageOverride(string[] args)
    {
        int index = Array.IndexOf(args, "--lang");
        if (index >= 0 && index + 1 < args.Length)
            AppSetup.TrySetCulture(args[index + 1]);
    }
}
