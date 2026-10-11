param(
    [ValidateSet('x64', 'ARM64')][string]$Platform = 'x64',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet build samples/Orchard.Catalog/Orchard.Catalog.csproj -c $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) { throw 'Catalog build failed.' }
    $rid = if ($Platform -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
    & "samples/Orchard.Catalog/bin/$Platform/$Configuration/net10.0-windows10.0.26100.0/$rid/Orchard.Catalog.exe"
} finally {
    Pop-Location
}
