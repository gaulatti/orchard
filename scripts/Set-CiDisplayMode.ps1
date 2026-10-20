# GitHub-hosted Windows CI only. Changes the current display mode dynamically;
# never saves registry/global settings, creates modes, changes DPI, or installs drivers.
param([string]$Output = 'artifacts/display-session.json')
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:GITHUB_ACTIONS -ne 'true' -or $env:CI -ne 'true' -or
    $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Display setup is restricted to this GitHub-hosted Windows CI job.'
}
if (-not ('OrchardCiDisplay.Native' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace OrchardCiDisplay {
    // Unicode DISPLAY_DEVICEW: 840 bytes on both x86 and x64.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Device {
        public uint Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }
    // Unicode DEVMODEW: use the 16-byte DISPLAY arm of the printer/display union.
    // The later dmDisplayFlags/dmNup union is one uint, not two fields.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Mode {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        public ushort SpecVersion, DriverVersion, Size, DriverExtra;
        public uint Fields;
        public int PositionX, PositionY;
        public uint DisplayOrientation, DisplayFixedOutput;
        public short Color, Duplex, YResolution, TTOption, Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FormName;
        public ushort LogPixels;
        public uint BitsPerPel, PelsWidth, PelsHeight, DisplayFlags, DisplayFrequency;
        public uint ICMMethod, ICMIntent, MediaType, DitherType;
        public uint Reserved1, Reserved2, PanningWidth, PanningHeight;
    }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct MonitorInfo {
        public uint Size; public Rect Bounds, Work; public uint Flags;
    }
    public sealed class Desktop {
        public int PrimaryWidth, PrimaryHeight, VirtualX, VirtualY, VirtualWidth, VirtualHeight;
        public MonitorInfo PrimaryMonitor;
    }
    public static class Native {
        [DllImport("user32.dll", EntryPoint = "EnumDisplayDevicesW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDevices(string device, uint index, ref Device value, uint flags);
        [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumMode(string device, int index, ref Mode value);
        [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int ChangeMode(string device, ref Mode mode, IntPtr window, uint flags, IntPtr parameter);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(Point point, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo value);
        public static void ValidateLayouts() {
            if (Marshal.SizeOf(typeof(Device)) != 840 || Marshal.SizeOf(typeof(Mode)) != 220 ||
                Marshal.OffsetOf(typeof(Mode), "Size").ToInt32() != 68 ||
                Marshal.OffsetOf(typeof(Mode), "Fields").ToInt32() != 72 ||
                Marshal.OffsetOf(typeof(Mode), "PelsWidth").ToInt32() != 172 ||
                Marshal.OffsetOf(typeof(Mode), "PelsHeight").ToInt32() != 176 ||
                Marshal.OffsetOf(typeof(Mode), "DisplayFrequency").ToInt32() != 184)
                throw new InvalidOperationException("Unexpected Unicode Win32 display struct layout.");
        }
        private static Mode EmptyMode() {
            return new Mode { Size = (ushort)Marshal.SizeOf(typeof(Mode)), DriverExtra = 0,
                DeviceName = String.Empty, FormName = String.Empty };
        }
        public static Device PrimaryDevice() {
            Device? primary = null;
            for (uint i = 0; ; i++) {
                var device = new Device { Size = (uint)Marshal.SizeOf(typeof(Device)),
                    DeviceName = String.Empty, DeviceString = String.Empty,
                    DeviceId = String.Empty, DeviceKey = String.Empty };
                if (!EnumDevices(null, i, ref device, 0)) break;
                // DISPLAY_DEVICE_ATTACHED_TO_DESKTOP | DISPLAY_DEVICE_PRIMARY_DEVICE.
                if ((device.StateFlags & 5) != 5) continue;
                if (primary.HasValue) throw new InvalidOperationException("Multiple primary displays reported.");
                primary = device;
            }
            if (!primary.HasValue) throw new InvalidOperationException("No attached primary display reported.");
            return primary.Value;
        }
        public static Mode Current(string device) {
            var mode = EmptyMode();
            if (!EnumMode(device, -1, ref mode))
                throw new InvalidOperationException("Cannot read ENUM_CURRENT_SETTINGS.");
            return mode;
        }
        public static Mode[] Advertised(string device) {
            var modes = new List<Mode>();
            for (int i = 0; ; i++) {
                var mode = EmptyMode();
                if (!EnumMode(device, i, ref mode)) break;
                if (mode.DriverExtra != 0)
                    throw new InvalidOperationException("Display requires unhandled private DEVMODE data.");
                modes.Add(mode);
            }
            return modes.ToArray();
        }
        public static int Test(string device, Mode mode) { return Set(device, mode, 2); } // CDS_TEST only.
        public static int Apply(string device, Mode mode) { return Set(device, mode, 0); } // Dynamic only.
        private static int Set(string device, Mode mode, uint flags) {
            mode.Size = (ushort)Marshal.SizeOf(typeof(Mode)); mode.DriverExtra = 0;
            // Only the five fields returned by EnumDisplaySettings are requested.
            // No orientation, topology, scaling, registry, global, or unsafe-mode flags.
            mode.Fields = 0x007C0000;
            mode.DisplayOrientation = 0; mode.DisplayFixedOutput = 0;
            return ChangeMode(device, ref mode, IntPtr.Zero, flags, IntPtr.Zero);
        }
        public static Desktop ObserveDesktop() {
            // Thread-scoped PMv2 only: query physical coordinates without changing system DPI.
            var previous = SetThreadDpiAwarenessContext(new IntPtr(-4));
            if (previous == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            try {
                var monitor = new MonitorInfo { Size = (uint)Marshal.SizeOf(typeof(MonitorInfo)) };
                if (!GetMonitorInfo(MonitorFromPoint(new Point(), 1), ref monitor) || (monitor.Flags & 1) == 0)
                    throw new InvalidOperationException("Cannot read primary monitor physical bounds.");
                return new Desktop { PrimaryWidth = GetSystemMetrics(0), PrimaryHeight = GetSystemMetrics(1),
                    VirtualX = GetSystemMetrics(76), VirtualY = GetSystemMetrics(77),
                    VirtualWidth = GetSystemMetrics(78), VirtualHeight = GetSystemMetrics(79), PrimaryMonitor = monitor };
            } finally {
                if (SetThreadDpiAwarenessContext(previous) == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
'@
}
$receipt = [ordered]@{ startedAtUtc = [DateTime]::UtcNow.ToString('O'); passed = $false }
$outputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath)) | Out-Null
try {
    [OrchardCiDisplay.Native]::ValidateLayouts()
    $device = [OrchardCiDisplay.Native]::PrimaryDevice()
    $receipt.device = $device
    $before = [OrchardCiDisplay.Native]::Current($device.DeviceName)
    $receipt.before = $before
    $receipt.desktopBefore = [OrchardCiDisplay.Native]::ObserveDesktop()
    $modes = @([OrchardCiDisplay.Native]::Advertised($device.DeviceName))
    $receipt.advertisedModes = $modes
    # Prefer the standard 1920x1080 viewport. Otherwise use only an advertised
    # progressive 32bpp mode with >=1600x1000 room; never fabricate a DEVMODE.
    $candidates = @($modes | Where-Object {
        $_.BitsPerPel -eq 32 -and $_.PelsWidth -ge 1600 -and $_.PelsHeight -ge 1000 -and
        ($_.DisplayFlags -band 2) -eq 0
    } | Sort-Object -Property @(
        @{ Expression = { if ($_.PelsWidth -eq 1920 -and $_.PelsHeight -eq 1080) { 0 } else { 1 } } },
        @{ Expression = { [long]$_.PelsWidth * [long]$_.PelsHeight } },
        @{ Expression = { if ($_.DisplayFrequency -eq $before.DisplayFrequency) { 0 } else { 1 } } },
        @{ Expression = { if ($_.DisplayFrequency -eq 60) { 0 } else { 1 } } },
        @{ Expression = { $_.DisplayFrequency } }
    ))
    if ($candidates.Count -eq 0) { throw 'No advertised progressive 32bpp display mode is adequate for the fixed native fixtures.' }
    $tests = @(); $selected = $null
    foreach ($candidate in $candidates) {
        $result = [OrchardCiDisplay.Native]::Test($device.DeviceName, $candidate)
        $tests += [ordered]@{ mode = $candidate; result = $result }
        if ($result -eq 0) { $selected = $candidate; break }
    }
    $receipt.tests = $tests
    if ($null -eq $selected) { throw 'All advertised adequate modes failed CDS_TEST; no desktop change attempted.' }
    $receipt.selected = $selected
    $apply = [OrchardCiDisplay.Native]::Apply($device.DeviceName, $selected)
    $receipt.applyResult = $apply
    # DISP_CHANGE_RESTART (1) is not success; this job never restarts Windows.
    if ($apply -ne 0) { throw "Dynamic display change failed with DISP_CHANGE result $apply." }
    $clock = [Diagnostics.Stopwatch]::StartNew()
    do {
        $after = [OrchardCiDisplay.Native]::Current($device.DeviceName)
        $desktop = [OrchardCiDisplay.Native]::ObserveDesktop()
        $bounds = $desktop.PrimaryMonitor.Bounds
        $ready = $after.PelsWidth -eq $selected.PelsWidth -and $after.PelsHeight -eq $selected.PelsHeight -and
            $after.BitsPerPel -eq 32 -and
            ($selected.DisplayFrequency -le 1 -or $after.DisplayFrequency -eq $selected.DisplayFrequency) -and
            $desktop.PrimaryWidth -eq $selected.PelsWidth -and $desktop.PrimaryHeight -eq $selected.PelsHeight -and
            ($bounds.Right - $bounds.Left) -eq $selected.PelsWidth -and
            ($bounds.Bottom - $bounds.Top) -eq $selected.PelsHeight
        if (-not $ready) { Start-Sleep -Milliseconds 100 }
    } while (-not $ready -and $clock.ElapsedMilliseconds -lt 10000)
    $receipt.after = $after; $receipt.desktopAfter = $desktop
    $receipt.settleMilliseconds = $clock.ElapsedMilliseconds
    if (-not $ready) { throw 'Display API returned success but the observed physical mode/bounds did not match.' }
    $receipt.passed = $true
} catch {
    $receipt.error = $_.Exception.Message
    throw
} finally {
    $receipt.finishedAtUtc = [DateTime]::UtcNow.ToString('O')
    $receipt | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $outputPath -Encoding utf8
}
$receipt | ConvertTo-Json -Depth 10
