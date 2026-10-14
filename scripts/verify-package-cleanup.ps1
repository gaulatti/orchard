param(
    [string]$Package = 'artifacts/packages/Gaulatti.Orchard.0.1.0.nupkg',
    [string]$Output = 'artifacts/package-cleanup-regression'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

# Invoke the real verifier against small, deliberately invalid consumer fixtures.
# Neither failure should leave the verifier's newly generated package cache.
$cases = @(
    @{ Name = 'project-reference'; Project = '<Project><ItemGroup><ProjectReference Include="forbidden.csproj" /></ItemGroup></Project>';
       Expected = 'Consumer must reference the package, never a project.' },
    @{ Name = 'restore-failure'; Project = '<Project'; Expected = 'Clean package consumer restore failed.' }
)
$observations = @()
foreach ($case in $cases) {
    $fixtureRoot = Join-Path $outputPath ($case.Name + '-fixture')
    $fixtureScripts = Join-Path $fixtureRoot 'scripts'
    $fixtureConsumer = Join-Path $fixtureRoot 'tests/PackageConsumer'
    New-Item -ItemType Directory -Path $fixtureScripts, $fixtureConsumer -Force | Out-Null
    $fixtureVerifier = Join-Path $fixtureScripts 'verify-package.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'verify-package.ps1') -Destination $fixtureVerifier -Force
    Copy-Item -LiteralPath (Join-Path $repository 'global.json') -Destination $fixtureRoot -Force
    Set-Content -LiteralPath (Join-Path $fixtureConsumer 'PackageConsumer.csproj') -Value $case.Project
    $caseOutput = Join-Path $outputPath $case.Name
    $failure = $null
    try { & $fixtureVerifier -Package $packagePath -Output $caseOutput -BuildOnly }
    catch { $failure = $_.Exception.Message }
    if ($failure -ne $case.Expected) { throw "Unexpected $($case.Name) result: $failure" }
    $cleanup = Get-Content -LiteralPath (Join-Path $caseOutput 'cleanup.json') -Raw | ConvertFrom-Json
    if (-not $cleanup.removed -or (Test-Path -LiteralPath $cleanup.isolatedConsumer)) {
        throw "$($case.Name) retained its temporary consumer workspace."
    }
    if (Test-Path -LiteralPath (Join-Path $caseOutput 'result.json')) {
        throw "$($case.Name) produced a successful verification report."
    }
    $observations += [ordered]@{ name = $case.Name; expectedFailure = $failure; cleanup = $cleanup }
}
[ordered]@{ passed = $true; cases = $observations } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputPath 'result.json')
Get-Content -LiteralPath (Join-Path $outputPath 'result.json')
