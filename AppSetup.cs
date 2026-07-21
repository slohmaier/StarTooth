using System.Globalization;

namespace StarTooth;

/// <summary>Applies the stored preferences to the running process.</summary>
internal static class AppSetup
{
    /// <summary>
    /// Applies the colour mode to both our own palette and the WinForms one. They must agree:
    /// the .NET dark mode paints a form's background itself, overriding an explicit BackColor, so
    /// leaving it on System while Theme says Light produces a light dialog on a dark background.
    /// </summary>
    internal static void ApplyColorMode(Settings settings)
    {
        Theme.Mode = settings.Theme;

#pragma warning disable WFO5001 // SetColorMode is experimental in .NET 9.
        Application.SetColorMode(settings.Theme switch
        {
            ThemeMode.Light => SystemColorMode.Classic,
            ThemeMode.Dark => SystemColorMode.Dark,
            _ => SystemColorMode.System,
        });
#pragma warning restore WFO5001
    }

    /// <summary>
    /// Switches the UI culture. Safe to call while running: every string is looked up through
    /// CurrentUICulture at the moment it is used, never cached at startup.
    /// </summary>
    internal static void ApplyLanguage(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Language))
        {
            // Back to whatever Windows says, which is what the thread started with.
            CultureInfo system = CultureInfo.InstalledUICulture;
            CultureInfo.DefaultThreadCurrentUICulture = system;
            CultureInfo.CurrentUICulture = system;
            return;
        }

        TrySetCulture(settings.Language);
    }

    /// <summary>Returns false for an unknown culture, leaving the current one untouched.</summary>
    internal static bool TrySetCulture(string name)
    {
        try
        {
            var culture = new CultureInfo(name);
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
