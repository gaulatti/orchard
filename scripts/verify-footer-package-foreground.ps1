param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$target = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $target) { throw 'Use a fresh Footer package foreground output directory.' }
New-Item -ItemType Directory -Path $target | Out-Null
$library = (Get-FileHash -LiteralPath (Join-Path $app 'Orchard.dll')).Hash.ToLowerInvariant()
$consumerHostHash = (Get-FileHash -LiteralPath (Join-Path $app 'PackageConsumer.dll')).Hash.ToLowerInvariant()
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | Where-Object { $_.FullName -match '^lib/[^/]+/Orchard\.dll$' })
    if ($entries.Count -ne 1) { throw 'Footer package must contain one Orchard library assembly.' }
    $stream = $entries[0].Open()
    try { $packagedLibrary = [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData($stream)) }
    finally { $stream.Dispose() }
}
finally { $archive.Dispose() }
if ($packagedLibrary -ne $library) { throw 'Consumer DLL differs from the NuGet package.' }

$reports = @()
foreach ($theme in @('Light', 'Dark')) {
    $reportPath = Join-Path $target "footer-foreground-$theme.json"
    $process = Start-Process -FilePath (Join-Path $app 'PackageConsumer.exe') -ArgumentList "--probe-footer-foreground --footer-theme $theme --verify-output `"$reportPath`"" -PassThru
    $exited = $process.WaitForExit(60000)
    [ordered]@{ processId = $process.Id; exited = $exited; exitCode = if ($exited) { $process.ExitCode } else { $null } } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target "process-$theme.json")
    if (-not $exited) { throw "Footer $theme process is still live; inspect PID $($process.Id)." }
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $reportPath)) { throw "Footer $theme foreground failed; inspect $reportPath." }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    foreach ($field in @('passed', 'originalItemInvokedByPointer', 'originalItemInvokedByKeyboard',
        'customRendererUsed', 'keyboardFocusRetained', 'callerInputRetained', 'themeBrandInkVerified',
        'pointerSent', 'keyboardSent')) {
        if ($report.$field -ne $true) { throw "Footer $theme report did not prove $field." }
    }
    if ($report.theme -ne $theme -or $report.processArchitecture -ne 'X64' -or $report.osArchitecture -ne 'X64' -or
        $report.foregroundOwnerPid -ne $process.Id -or $report.librarySha256 -ne $library -or $report.hostSha256 -ne $consumerHostHash) {
        throw "Footer $theme identity or foreground ownership differed from the package consumer."
    }
    $reports += [ordered]@{ theme = $theme; report = $reportPath; sha256 = (Get-FileHash -LiteralPath $reportPath).Hash.ToLowerInvariant(); processId = $process.Id }
}
[ordered]@{
    passed = $true
    component = 'BleeckerFooter'
    scope = 'x64-package-consumer-physical-pointer-keyboard-light-dark'
    packageSha256 = (Get-FileHash -LiteralPath $packagePath).Hash.ToLowerInvariant()
    librarySha256 = $library
    hostSha256 = $consumerHostHash
    reports = $reports
    nativeArm64RuntimeVerified = $false
    nativeComponentAccepted = $false
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $target 'footer-foreground-audit.json')
Write-Output 'Footer package foreground: Light/Dark pointer and keyboard passed.'
