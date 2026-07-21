using Microsoft.Win32;

namespace StarTooth;

/// <summary>
/// Autostart via the per-user Run key. The registry is the single source of truth, so a value
/// removed by hand or by another tool is reflected immediately rather than being overwritten from
/// a stale copy in the settings file.
/// </summary>
internal static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "StarTooth";

    internal static bool IsEnabled
    {
        get
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) is string value && value.Length > 0;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>Returns false if the registry refused the change, so the UI can stay truthful.</summary>
    internal static bool TrySet(bool enabled)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key is null)
                return false;

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return true;
            }

            string? path = Environment.ProcessPath;
            if (string.IsNullOrEmpty(path))
                return false;

            // Quoted so a path containing spaces survives the shell.
            key.SetValue(ValueName, $"\"{path}\"", RegistryValueKind.String);
            return true;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
