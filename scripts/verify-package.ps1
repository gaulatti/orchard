param(
    [string]$Package = 'artifacts/packages/Gaulatti.Orchard.0.1.0.nupkg',
    [ValidateSet('x64', 'ARM64')][string]$Platform = 'x64',
    [string]$Output = 'artifacts/package-consumer',
    [switch]$BuildOnly
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
foreach ($reportName in @('result.json', 'runtime.json')) {
    $reportPath = Join-Path $outputPath $reportName
    if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath }
}
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | ForEach-Object FullName)
    foreach ($required in @('buildTransitive/Gaulatti.Orchard.targets', 'README.md', 'licenses/LICENSE-Bleecker.txt', 'licenses/LICENSE-Lucide.txt', 'licenses/LICENSE-Radix.txt', 'licenses/LICENSE-D3-Shape.txt', 'licenses/LICENSE-EsToolkit.txt', 'licenses/LICENSE-Recharts.txt',
        'assets/fonts/EncodeSans-Regular.ttf', 'assets/fonts/EncodeSans-Medium.ttf', 'assets/fonts/EncodeSans-SemiBold.ttf',
        'assets/fonts/LibreFranklin-Regular.ttf', 'assets/fonts/LibreFranklin-SemiBold.ttf',
        'assets/fonts/OFL-EncodeSans.txt', 'assets/fonts/OFL-LibreFranklin.txt', 'assets/images/gaulatti-logo.svg')) {
        if ($required -notin $entries) { throw "Missing package entry: $required" }
    }
    if (@($entries | Where-Object { $_ -match '\.ttf$' }).Count -ne 5) { throw 'Package must contain each bundled font exactly once.' }
    if ($entries | Where-Object { $_ -match 'Catalog|Tests|\.cs$' }) { throw 'Application-facing package contains development infrastructure.' }
    $assemblyEntries = @($archive.Entries | Where-Object { $_.FullName -match '^lib/[^/]+/Orchard\.dll$' })
    if ($assemblyEntries.Count -ne 1) { throw 'Package must expose exactly one portable Orchard assembly.' }
    $assemblyStream = [IO.MemoryStream]::new()
    $entryStream = $assemblyEntries[0].Open()
    try { $entryStream.CopyTo($assemblyStream) } finally { $entryStream.Dispose() }
    $assemblyStream.Position = 0
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($assemblyStream)
    try {
        if ($pe.PEHeaders.CoffHeader.Machine -ne [System.Reflection.PortableExecutable.Machine]::I386 -or
            ($pe.PEHeaders.CorHeader.Flags -band [System.Reflection.PortableExecutable.CorFlags]::Requires32Bit) -ne 0 -or
            ($pe.PEHeaders.CorHeader.Flags -band [System.Reflection.PortableExecutable.CorFlags]::ILOnly) -eq 0) {
            throw 'Orchard.dll must be AnyCPU IL so one package can serve x64 and ARM64 consumers.'
        }
    }
    finally { $pe.Dispose(); $assemblyStream.Dispose() }
    $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName -like '*.nuspec' })
    if ($nuspecEntries.Count -ne 1) { throw 'Package must contain exactly one nuspec.' }
    $nuspecEntry = $nuspecEntries[0]
    $reader = [IO.StreamReader]::new($nuspecEntry.Open())
    try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $version = $nuspec.package.metadata.version
}
finally { $archive.Dispose() }

# A new directory and cache ensure a previously packed version cannot satisfy this test.
# No parent repository props, source projects or sibling checkouts are copied.
$consumerRoot = Join-Path ([IO.Path]::GetTempPath()) ('orchard-consumer-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $consumerRoot | Out-Null
$consumerSource = Join-Path $consumerRoot 'source'
New-Item -ItemType Directory -Path $consumerSource | Out-Null
Get-ChildItem -LiteralPath "$repository/tests/PackageConsumer" -File | Copy-Item -Destination $consumerSource
Copy-Item -LiteralPath "$repository/global.json" -Destination $consumerRoot
$feed = Join-Path $consumerRoot 'feed'
New-Item -ItemType Directory -Path $feed | Out-Null
Copy-Item -LiteralPath $packagePath -Destination $feed
$project = Join-Path $consumerSource 'PackageConsumer.csproj'
if ((Get-Content -LiteralPath $project -Raw) -match 'ProjectReference') { throw 'Consumer must reference the package, never a project.' }
$config = [xml]'<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear/><add key="orchard" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="orchard"><package pattern="Gaulatti.Orchard"/></packageSource><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>'
$config.configuration.packageSources.add[0].SetAttribute('value', $feed)
$configPath = Join-Path $consumerRoot 'NuGet.Config'
$config.Save($configPath)
Push-Location $consumerSource
try {
    dotnet restore $project --configfile $configPath --packages "$consumerRoot/packages" -p:Platform=$Platform -p:OrchardPackageVersion=$version
    if ($LASTEXITCODE -ne 0) { throw 'Clean package consumer restore failed.' }
    dotnet publish $project -c Release -p:Platform=$Platform -p:OrchardPackageVersion=$version --no-restore -o "$outputPath/app"
    if ($LASTEXITCODE -ne 0) { throw 'Clean package consumer publish failed.' }
}
finally { Pop-Location }
$restoredPackage = "$consumerRoot/packages/gaulatti.orchard/$version/gaulatti.orchard.$version.nupkg"
if ((Get-FileHash -LiteralPath $packagePath).Hash -ne (Get-FileHash -LiteralPath $restoredPackage).Hash) {
    throw 'Consumer restored a different package from the one under test.'
}
if ($BuildOnly) {
    [ordered]@{ buildPassed = $true; runtimeVerified = $false; version = $version; platform = $Platform;
        packageSha256 = (Get-FileHash -LiteralPath $packagePath).Hash.ToLowerInvariant(); isolatedConsumer = $consumerRoot
    } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/result.json"
    Get-Content -LiteralPath "$outputPath/result.json"
    return
}
$runtimeReport = Join-Path $outputPath 'runtime.json'
$process = Start-Process -FilePath "$outputPath/app/PackageConsumer.exe" -ArgumentList "--verify-output `"$runtimeReport`"" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Package consumer runtime verification timed out.' }
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $runtimeReport) { Get-Content -LiteralPath $runtimeReport }
    throw "Package consumer exited with code $($process.ExitCode)."
}
$runtime = Get-Content -LiteralPath $runtimeReport -Raw | ConvertFrom-Json
if (-not $runtime.passed) { throw 'Package consumer reported failure.' }
if ($runtime.architecture -ne $Platform -or ($Platform -eq 'ARM64' -and $runtime.osArchitecture -ne 'Arm64')) {
    throw "Package consumer requires $Platform runtime; reported process=$($runtime.architecture), OS=$($runtime.osArchitecture)."
}
[ordered]@{
    passed = $true
    version = $version
    packageSha256 = (Get-FileHash -LiteralPath $packagePath).Hash.ToLowerInvariant()
    platform = $Platform
    isolatedConsumer = $consumerRoot
    runtime = $runtime
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$outputPath/result.json"
Get-Content -LiteralPath "$outputPath/result.json"
