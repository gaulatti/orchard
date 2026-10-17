$ErrorActionPreference = 'Stop'
if (-not ('OrchardMotionPreference' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class OrchardMotionPreference {
    [DllImport("user32.dll", EntryPoint="SystemParametersInfoW", SetLastError=true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadSetting(uint action, uint parameter, [MarshalAs(UnmanagedType.Bool)] out bool value, uint flags);
    [DllImport("user32.dll", EntryPoint="SystemParametersInfoW", SetLastError=true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteSetting(uint action, uint parameter, IntPtr value, uint flags);
    public static bool Read() {
        if (!ReadSetting(0x1042, 0, out bool value, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return value;
    }
    public static void Set(bool enabled) {
        if (!WriteSetting(0x1043, 0, new IntPtr(enabled ? 1 : 0), 2)) throw new Win32Exception(Marshal.GetLastWin32Error());
        if (Read() != enabled) throw new InvalidOperationException("Windows did not apply the animation preference.");
    }
}
'@
}
