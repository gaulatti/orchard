param(
    [ValidateSet('direct-null','metadata-null')]
    [string]$RestoreMode = 'direct-null',
    [switch]$ReadOnly,
    [string]$Output = 'artifacts/native-null-scheme-probe/result.json'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Native contrast probing requires an isolated GitHub-hosted Windows runner.'
}
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class NativeContrastProbe {
    public sealed class State {
        public uint Flags { get; }
        public string Scheme { get; }
        public bool Enabled => (Flags & 1u) != 0;
        public State(uint flags, string scheme) { Flags = flags; Scheme = scheme; }
    }
    [StructLayout(LayoutKind.Sequential)] private struct HighContrast {
        public uint Size, Flags; public IntPtr Scheme;
    }
    [DllImport("user32.dll", EntryPoint="SystemParametersInfoW", SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action,uint size,ref HighContrast value,uint flags);
    [DllImport("user32.dll")] private static extern uint GetSysColor(int index);
    public static State Read() {
        var value = new HighContrast { Size=(uint)Marshal.SizeOf<HighContrast>() };
        if (!SystemParametersInfo(0x42,value.Size,ref value,0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        // Match the catalog's tested immediate-copy getter. No buffer ownership
        // experiment is part of these setter-flag probes.
        return new State(value.Flags,value.Scheme==IntPtr.Zero?null:Marshal.PtrToStringUni(value.Scheme));
    }
    public static void Write(State state,uint outerFlags) {
        var pointer=state.Scheme==null?IntPtr.Zero:Marshal.StringToHGlobalUni(state.Scheme);
        try {
            var value=new HighContrast { Size=(uint)Marshal.SizeOf<HighContrast>(),Flags=state.Flags,Scheme=pointer };
            if (!SystemParametersInfo(0x43,value.Size,ref value,outerFlags)) throw new Win32Exception(Marshal.GetLastWin32Error());
        } finally { if(pointer!=IntPtr.Zero) Marshal.FreeHGlobal(pointer); }
    }
    public static void WriteNullScheme(uint flags) { Write(new State(flags,null),2u); }
    public static void Enable(State original) { Write(new State(original.Flags | 1u,original.Scheme),2u); }
    public static bool Equal(State first,State second) => first.Flags==second.Flags && String.Equals(first.Scheme,second.Scheme,StringComparison.Ordinal);
    public static uint[] Palette() => new uint[] { GetSysColor(5),GetSysColor(8),GetSysColor(13),GetSysColor(14) };
}
'@
function Read-ContrastRegistry {
    $path='HKCU:\Control Panel\Accessibility\HighContrast'
    $values=[ordered]@{}
    if (-not (Test-Path $path)) { return $values }
    $key=Get-Item $path
    try {
        foreach ($name in @('Flags','High Contrast Scheme')) {
            $present=@($key.GetValueNames()) -contains $name
            $values[$name]=[ordered]@{ present=$present; kind=if ($present) { $key.GetValueKind($name).ToString() } else { $null }; value=if ($present) { $key.GetValue($name) } else { $null } }
        }
    } finally { $key.Dispose() }
    return $values
}
if ($ReadOnly) {
    [ordered]@{ native=[NativeContrastProbe]::Read(); palette=[NativeContrastProbe]::Palette(); registry=Read-ContrastRegistry } | ConvertTo-Json -Depth 8 -Compress
    return
}
function Read-FreshProcess {
    $raw=& (Join-Path $PSHOME 'pwsh.exe') -NoLogo -NoProfile -NonInteractive -File $PSCommandPath -ReadOnly 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Fresh-process native read failed: $raw" }
    return ([string]::Join([Environment]::NewLine,[string[]]$raw) | ConvertFrom-Json)
}
function Same-Palette([uint[]]$First,[uint[]]$Second) {
    if ($First.Length -ne $Second.Length) { return $false }
    for ($index=0; $index -lt $First.Length; $index++) { if ($First[$index] -ne $Second[$index]) { return $false } }
    return $true
}
$flags = 2u
$original = [NativeContrastProbe]::Read()
$originalPalette = [NativeContrastProbe]::Palette()
if ($original.Enabled -or ($original.Flags -band 2u) -eq 0 -or $original.Scheme -cne '') { throw 'Probe requires contrast available, disabled and the exact empty original scheme.' }
for ($index=0; $index -lt 32; $index++) {
    if (-not [NativeContrastProbe]::Equal($original,[NativeContrastProbe]::Read())) { throw 'Initial native state is not stable.' }
}
$report = [ordered]@{
    diagnosticOnly=$true
    componentAcceptance=$false
    restoreMode=$RestoreMode
    restoreOuterFlags=$flags
    original=$original
    originalPalette=$originalPalette
    processArchitecture=[Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    passed=$false
}
$changed = $false
try {
    $report.originalFreshProcess=Read-FreshProcess
    $changed = $true
    [NativeContrastProbe]::Enable($original)
    $clock = [Diagnostics.Stopwatch]::StartNew()
    do {
        $active = [NativeContrastProbe]::Read()
        $activePalette = [NativeContrastProbe]::Palette()
        $activeObserved = $active.Enabled -and -not (Same-Palette $originalPalette $activePalette)
        if ($activeObserved) { break }
        Start-Sleep -Milliseconds 50
    } while ($clock.ElapsedMilliseconds -lt 5000)
    $report.active=$active; $report.activePalette=$activePalette
    if (-not $activeObserved) { throw 'Probe did not observe actual enabled native contrast and changed Windows colors.' }
    # Experimental request representation only. Keep the actual original empty
    # string as the immutable expected state; null is never a passing alias.
    $report.experimentalNullPointer=$true
    if ($RestoreMode -eq 'metadata-null') {
        [NativeContrastProbe]::Write($original,2u)
        $report.ordinaryEmptyStringImmediate=[NativeContrastProbe]::Read()
        $clock.Restart()
        do {
            $ordinary=[NativeContrastProbe]::Read()
            $ordinaryPalette=[NativeContrastProbe]::Palette()
            $ready=-not $ordinary.Enabled -and $ordinary.Flags -eq $original.Flags -and (Same-Palette $originalPalette $ordinaryPalette)
            if ($ready) { break }
            Start-Sleep -Milliseconds 50
        } while ($clock.ElapsedMilliseconds -lt 5000)
        $report.ordinaryEmptyStringRestore=$ordinary
        $report.ordinaryEmptyStringPalette=$ordinaryPalette
        if (-not $ready) {
            throw 'Metadata-null probe requires the ordinary mode/palette restoration to finish first.'
        }
        [NativeContrastProbe]::WriteNullScheme(($original.Flags -bor 0x1000u))
    } else {
        [NativeContrastProbe]::WriteNullScheme($original.Flags)
    }
    $report.immediateRestore=[NativeContrastProbe]::Read()
    $clock.Restart()
    do {
        $restored = [NativeContrastProbe]::Read()
        $restoredPalette = [NativeContrastProbe]::Palette()
        $paletteRestored = -not $restored.Enabled -and (Same-Palette $originalPalette $restoredPalette)
        $exactRestored = [NativeContrastProbe]::Equal($original,$restored)
        if ($paletteRestored -and $exactRestored) { break }
        Start-Sleep -Milliseconds 50
    } while ($clock.ElapsedMilliseconds -lt 5000)
    $report.settledRestore=$restored; $report.restoredPalette=$restoredPalette
    $report.settledFreshProcess=Read-FreshProcess
    $report.paletteRestored=$paletteRestored
    $report.exactStateRestored=$exactRestored
    $report.freshStateRestored=$report.settledFreshProcess.native.Flags -eq $original.Flags -and $report.settledFreshProcess.native.Scheme -ceq $original.Scheme
    $report.freshPaletteRestored=Same-Palette $originalPalette $report.settledFreshProcess.palette
    $report.registryRestored=($report.originalFreshProcess.registry | ConvertTo-Json -Depth 8 -Compress) -ceq ($report.settledFreshProcess.registry | ConvertTo-Json -Depth 8 -Compress)
    $report.passed=$paletteRestored -and $report.exactStateRestored -and $report.freshStateRestored -and $report.freshPaletteRestored -and $report.registryRestored
} catch {
    $report.error=$_.Exception.ToString()
} finally {
    # Best-effort safety cleanup never replaces the measured first result.
    if ($changed) {
        try {
            if ([NativeContrastProbe]::Read().Enabled) { [NativeContrastProbe]::Write($original,2u) }
            $report.afterCleanup=[NativeContrastProbe]::Read()
            if (-not [NativeContrastProbe]::Equal($original,$report.afterCleanup)) { $report.passed=$false }
        } catch { $report.cleanupError=$_.Exception.ToString(); $report.passed=$false }
    }
    $path=[IO.Path]::GetFullPath($Output)
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($path)) | Out-Null
    $json=$report | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText($path,$json)
    Write-Host $json
}
if (-not $report.passed) { throw 'Native contrast roundtrip did not restore its exact original state; diagnostic retained.' }
