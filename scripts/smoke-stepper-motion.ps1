param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/stepper-motion',
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture
)
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
$original = [OrchardMotionPreference]::Read()
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
try {
    foreach ($enabled in @($true,$false)) {
        [OrchardMotionPreference]::Set($enabled)
        $mode = if ($enabled) { 'enabled' } else { 'reduced' }
        $arguments = @{ Executable=$Executable; Output=(Join-Path $outputPath $mode); Component='stepper-motion'; ClientSize=@(1024,745) }
        if ($ExpectedArchitecture) { $arguments.ExpectedArchitecture=$ExpectedArchitecture }
        & "$PSScriptRoot/smoke-catalog.ps1" @arguments
        $report = Get-Content (Join-Path $arguments.Output 'result.json') -Raw | ConvertFrom-Json
        if ($report.environment.animationsEnabled -ne $enabled) { throw "WinRT animation preference differs from the $mode test scope." }
    }
}
finally {
    [OrchardMotionPreference]::Set($original)
    [ordered]@{ original=$original; restored=[OrchardMotionPreference]::Read(); passed=([OrchardMotionPreference]::Read() -eq $original) } |
        ConvertTo-Json | Set-Content (Join-Path $outputPath 'preference-restored.json')
}
