# Loaded before starting the fixture, so compiling interop cannot race its first capture.
if (-not ('OrchardTest.CaptureWindow' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace OrchardTest {
    public static class CaptureWindow {
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr window, uint attribute, ref int value, uint size);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr window, uint attribute, out int value, uint size);
        [DllImport("dwmapi.dll")] public static extern int DwmFlush();
    }
}
'@
}
function Set-CatalogCaptureWindow {
    param([Diagnostics.Process]$Process, [string]$Output, [Diagnostics.Stopwatch]$RuntimeClock, [int]$TimeoutMilliseconds)
    $taskClock = [Diagnostics.Stopwatch]::StartNew()
    [uint32]$taskOwner = 0
    do {
        $Process.Refresh()
        if ($Process.HasExited) { throw 'Catalog exited before capture-window preparation.' }
        $taskWindow = $Process.MainWindowHandle
        $taskOwner = 0
        if ($taskWindow -ne [IntPtr]::Zero) { [OrchardTest.CaptureWindow]::GetWindowThreadProcessId($taskWindow, [ref]$taskOwner) | Out-Null }
        if ($taskWindow -eq [IntPtr]::Zero -or $taskOwner -ne $Process.Id) {
            if ($RuntimeClock.ElapsedMilliseconds -ge $TimeoutMilliseconds) { throw 'Catalog did not expose its owned capture window within the runtime watchdog.' }
            Start-Sleep -Milliseconds 10
        }
    } while ($taskWindow -eq [IntPtr]::Zero -or $taskOwner -ne $Process.Id)
    $taskAvailable = [Environment]::OSVersion.Version.Build -ge 22000
    $taskBefore = $null; $taskAfter = $null
    if ($taskAvailable) {
        [int]$taskOriginal = 0
        if ([OrchardTest.CaptureWindow]::DwmGetWindowAttribute($taskWindow,33,[ref]$taskOriginal,4) -lt 0) { throw 'Could not read fixture window corner preference.' }
        [int]$taskPreference = 1 # DWMWCP_DONOTROUND: compare the complete rectangular client with the browser viewport.
        if ([OrchardTest.CaptureWindow]::DwmSetWindowAttribute($taskWindow,33,[ref]$taskPreference,4) -lt 0) { throw 'Could not square fixture window corners.' }
        [int]$taskObserved = 0
        if ([OrchardTest.CaptureWindow]::DwmGetWindowAttribute($taskWindow,33,[ref]$taskObserved,4) -lt 0 -or $taskObserved -ne 1) { throw 'Fixture window did not retain square corners.' }
        if ([OrchardTest.CaptureWindow]::DwmFlush() -lt 0) { throw 'Fixture window corner update did not reach DWM.' }
        $taskBefore = $taskOriginal; $taskAfter = $taskObserved
    }
    # Windows 10 predates this rounding API. No desktop-wide setting is changed.
    [ordered]@{processId=$Process.Id;window=$taskWindow.ToInt64();windowWaitMilliseconds=$taskClock.ElapsedMilliseconds;roundedCornersApiAvailable=$taskAvailable;preferenceBefore=$taskBefore;preferenceAfter=$taskAfter;configuredAtUtc=[DateTime]::UtcNow.ToString('O')} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'capture-window.json')
}
