param(
    [string]$ReferenceRoot = (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.cache/orchard/reference'),
    [string]$NodeExecutable = 'node',
    [string]$NpmCli = (Join-Path (Split-Path (Get-Command npm.cmd -ErrorAction Stop).Source -Parent) 'node_modules/npm/bin/npm-cli.js')
)
$ErrorActionPreference = 'Stop'
$reference = [IO.Path]::GetFullPath($ReferenceRoot)
$pin = 'a86059013cd59684ce452849278ea81e734f2066'
$major = & $NodeExecutable -p 'process.versions.node.split(".")[0]'
if ($LASTEXITCODE -ne 0 -or [int]$major -lt 24) { throw 'Reference preparation requires Node.js 24 or newer.' }
New-Item -ItemType Directory -Force -Path $reference | Out-Null
$gitDirectory = Join-Path $reference 'bleecker.git'
if (-not (Test-Path -LiteralPath $gitDirectory)) {
    git init --bare $gitDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Reference repository initialization failed.' }
}
git -C $gitDirectory fetch --depth=1 --filter=blob:none https://github.com/gaulatti/bleecker.git $pin
if ($LASTEXITCODE -ne 0) { throw 'Pinned reference fetch failed.' }
$archive = Join-Path $reference 'bleecker-source.zip'
git -C $gitDirectory archive --format=zip --output=$archive $pin
if ($LASTEXITCODE -ne 0) { throw 'Pinned source archive failed.' }
$source = Join-Path $reference 'source'
[IO.Compression.ZipFile]::ExtractToDirectory($archive, $source, $true)
Push-Location $source
try {
    & $NodeExecutable $NpmCli ci --omit=dev --ignore-scripts --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Pinned production dependency restore failed.' }
} finally { Pop-Location }
$tooling = Join-Path $reference 'tooling'
New-Item -ItemType Directory -Force -Path $tooling | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'date-controls-tooling-package.json') -Destination (Join-Path $tooling 'package.json')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'date-controls-tooling-lock.json') -Destination (Join-Path $tooling 'package-lock.json')
Push-Location $tooling
try {
    & $NodeExecutable $NpmCli ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Locked reference compiler restore failed.' }
} finally { Pop-Location }
[ordered]@{ source = $source; tooling = $tooling; commit = $pin } | ConvertTo-Json
