using Microsoft.Win32;

namespace StarTooth;

/// <summary>
/// System light/dark colours. Windows exposes the app theme only through the registry, so it is
/// read on demand and re-read when the shell reports a preference change.
/// </summary>
internal static class Theme
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private static bool? _isDark;
    private static ThemeMode _mode = ThemeMode.System;

    /// <summary>Overrides the Windows setting when set to something other than System.</summary>
    internal static ThemeMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            Invalidate();
        }
    }

    internal static bool IsDark => Mode switch
    {
        ThemeMode.Light => false,
        ThemeMode.Dark => true,
        _ => _isDark ??= ReadIsDark(),
    };

    /// <summary>Drops the cached value so the next read picks up a theme switch.</summary>
    internal static void Invalidate() => _isDark = null;

    private static bool ReadIsDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            // AppsUseLightTheme is 0 in dark mode. A missing value means the classic light theme.
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal static Color Background => IsDark ? Color.FromArgb(32, 32, 32) : Color.White;
    internal static Color Foreground => IsDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(20, 20, 20);
    internal static Color DisabledForeground => IsDark ? Color.FromArgb(140, 140, 140) : Color.FromArgb(120, 120, 120);
    internal static Color Highlight => IsDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 235, 252);
    internal static Color Border => IsDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(200, 200, 200);
    internal static Color Separator => IsDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(215, 215, 215);
}
