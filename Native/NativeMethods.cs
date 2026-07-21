using System.Runtime.InteropServices;

namespace StarTooth;

internal static partial class NativeMethods
{
    [LibraryImport("user32.dll", EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(IntPtr hIcon);
}
