param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Archive,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][ValidateSet('x64', 'ARM64')][string]$Platform,
    [Parameter(Mandatory)][string]$Version
)
$ErrorActionPreference = 'Stop'
$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$zipPath = [IO.Path]::GetFullPath($Archive)
$reportPath = [IO.Path]::GetFullPath($Output)
if ((Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath $reportPath)) { throw 'Use fresh catalog archive and report paths.' }
$archiveFolder = Split-Path $zipPath -Parent
New-Item -ItemType Directory -Path $archiveFolder -Force | Out-Null
$runtime = if ($Platform -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
$deps = Get-Content -LiteralPath (Join-Path $app 'Orchard.Catalog.deps.json') -Raw | ConvertFrom-Json
if (-not $deps.runtimeTarget.name.EndsWith('/' + $runtime, [StringComparison]::Ordinal)) { throw 'Catalog runtime target does not match the requested platform.' }

# Build output contains an older nested publish directory. Zip only the current
# app tree, and verify every archive entry against its source without copying it.
$files = @(Get-ChildItem -LiteralPath $app -Recurse -File | Where-Object {
    $relative = [IO.Path]::GetRelativePath($app, $_.FullName).Replace('\', '/')
    -not $relative.StartsWith('publish/', [StringComparison]::OrdinalIgnoreCase)
} | Sort-Object FullName)
if ($files.Count -eq 0 -or @($files | Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 }).Count) { throw 'Catalog input is empty or contains reparse-point files.' }
$paths = @($files | ForEach-Object { [IO.Path]::GetRelativePath($app, $_.FullName).Replace('\', '/') })
foreach ($required in @('Orchard.Catalog.exe', 'Orchard.Catalog.dll', 'Orchard.dll', 'resources.pri', 'AuthShellsFixture.xbf', 'CommandSpotlightsFixture.xbf')) {
    if ($required -notin $paths) { throw "Missing catalog artifact entry: $required" }
}
$source = Join-Path $PSScriptRoot '../samples/Orchard.Catalog'
$expectedXbf = @(
    Get-ChildItem -LiteralPath $source -Filter '*.xaml' -File | ForEach-Object { "$($_.BaseName).xbf" }
    Get-ChildItem -LiteralPath (Join-Path $source 'Demos') -Filter '*.xaml' -File | ForEach-Object { "Demos/$($_.BaseName).xbf" }
)
$actualXbf = @($paths | Where-Object { $_.EndsWith('.xbf', [StringComparison]::OrdinalIgnoreCase) })
if (@(Compare-Object $expectedXbf $actualXbf -CaseSensitive).Count) { throw 'Catalog XBF set does not match current fixture source.' }

$fileRecords = @()
$zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    for ($index = 0; $index -lt $files.Count; $index++) {
        $file = $files[$index]
        $relative = $paths[$index]
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal)
        $fileRecords += [ordered]@{ path = $relative; bytes = $file.Length; sha256 = (Get-FileHash -LiteralPath $file.FullName).Hash.ToLowerInvariant() }
    }
}
finally { $zip.Dispose() }
$check = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($check.Entries.Count -ne $fileRecords.Count) { throw 'Catalog archive entry count differs from the built app.' }
    foreach ($record in $fileRecords) {
        $entry = $check.GetEntry($record.path)
        if ($null -eq $entry -or $entry.Length -ne $record.bytes) { throw "Catalog archive entry missing or truncated: $($record.path)" }
        $stream = $entry.Open()
        try { $digest = [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData($stream)) }
        finally { $stream.Dispose() }
        if ($digest -ne $record.sha256) { throw "Catalog archive bytes differ: $($record.path)" }
    }
}
finally { $check.Dispose() }
[ordered]@{
    format = 'orchard-catalog-build-archive-v1'
    version = $Version
    platform = $Platform
    runtime = $runtime
    archive = $zipPath
    archiveSha256 = (Get-FileHash -LiteralPath $zipPath).Hash.ToLowerInvariant()
    archiveBytes = (Get-Item -LiteralPath $zipPath).Length
    fileCount = $fileRecords.Count
    xbfCount = $actualXbf.Count
    allArchiveBytesVerified = $true
    nestedPublishExcluded = $true
    librarySha256 = (Get-FileHash -LiteralPath (Join-Path $app 'Orchard.dll')).Hash.ToLowerInvariant()
    catalogSha256 = (Get-FileHash -LiteralPath (Join-Path $app 'Orchard.Catalog.dll')).Hash.ToLowerInvariant()
    extractedRuntimeVerified = $false
    nativeArm64RuntimeVerified = $false
    nativeComponentAccepted = $false
    files = $fileRecords
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath
Write-Output "Catalog $Platform archived and stream-verified: $($fileRecords.Count) files."
