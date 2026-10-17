param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$X64,
    [Parameter(Mandatory)][string]$Arm64,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$x64Path = (Resolve-Path -LiteralPath $X64).Path
$arm64Path = (Resolve-Path -LiteralPath $Arm64).Path
$packageSha = (Get-FileHash -LiteralPath $packagePath).Hash.ToLowerInvariant()
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | Where-Object FullName -Match '^lib/[^/]+/Orchard\.dll$')
    Require ($entries.Count -eq 1) 'Expected one packaged Orchard library.'
    $stream = $entries[0].Open()
    try { $assemblySha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() }
    finally { $stream.Dispose() }
}
finally { $archive.Dispose() }
$result = Get-Content -LiteralPath "$x64Path/result.json" -Raw | ConvertFrom-Json
Require ($result.passed -and $result.platform -eq 'x64') 'Independent x64 consumer did not pass.'
Require ($result.packageSha256 -eq $packageSha) 'x64 consumer package differs.'
$armResult = Get-Content -LiteralPath "$arm64Path/result.json" -Raw | ConvertFrom-Json
Require ($armResult.buildPassed -and -not $armResult.runtimeVerified -and $armResult.platform -eq 'ARM64') 'Independent ARM64 build evidence is missing.'
Require ($armResult.packageSha256 -eq $packageSha) 'ARM64 consumer package differs.'
foreach ($consumerPath in @($x64Path, $arm64Path)) {
    Require (((Get-FileHash -LiteralPath "$consumerPath/app/Orchard.dll").Hash.ToLowerInvariant()) -eq $assemblySha) 'Consumer library bytes differ from the package.'
    $cleanup = Get-Content -LiteralPath "$consumerPath/cleanup.json" -Raw | ConvertFrom-Json
    Require ($cleanup.removed -and -not (Test-Path -LiteralPath $cleanup.isolatedConsumer)) 'Temporary consumer source/feed/cache was retained.'
}
$nativePairs = 0
foreach ($theme in @('Light', 'Dark')) {
    $dates = Get-Content -LiteralPath "$x64Path/consumer-dates-$theme.json" -Raw | ConvertFrom-Json
    Require ($dates.passed -and $dates.requests -eq 3 -and $dates.phases.Count -eq 7) "Packaged dates failed in $theme."
    $keys = @($dates.phases | Where-Object phase -EQ 'native-key')
    Require ($keys.Count -eq 4 -and (($keys.key -join ',') -eq '32,32,13,32')) 'Date activation keys differ.'
    foreach ($key in $keys) { Require ($key.downs -eq 1 -and $key.ups -eq 1 -and $key.exact) 'Date key lost native ownership.'; $nativePairs++ }
    $calendar = @($dates.phases | Where-Object phase -EQ 'calendar')
    $picker = @($dates.phases | Where-Object phase -EQ 'picker')
    $range = @($dates.phases | Where-Object phase -EQ 'range')
    Require ($calendar.Count -eq 1 -and $calendar[0].height -eq 252) 'Packaged calendar geometry differs.'
    Require (([DateTimeOffset]$calendar[0].value).Date -eq [datetime]'2026-10-19' -and ([DateTimeOffset]$picker[0].value).Date -eq [datetime]'2026-10-19') 'Packaged selected dates differ.'
    Require ($picker.Count -eq 1 -and $picker[0].focusRestored) 'Packaged picker lost focus restoration.'
    Require ($range.Count -eq 1) 'Packaged range request is missing.'
    $fromDate = [DateOnly]::ParseExact($range[0].value.From, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    $toDate = [DateOnly]::ParseExact($range[0].value.To, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    Require (($toDate.DayNumber - $fromDate.DayNumber) -eq 7) 'Packaged preset lost calendar-day subtraction.'
}
[ordered]@{ passed = $true; version = $result.version; packageSha256 = $packageSha; assemblySha256 = $assemblySha;
    nativeDateKeyPairs = $nativePairs; consumerRequests = 6; x64RuntimeVerified = $true; arm64BuildVerified = $true;
    arm64RuntimeVerified = $false; temporaryConsumersRemoved = $true; nativeComponentAccepted = $false } |
    ConvertTo-Json | Set-Content -LiteralPath $Output
Get-Content -LiteralPath $Output
