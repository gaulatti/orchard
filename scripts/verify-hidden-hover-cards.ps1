param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Catalog','PackageConsumer')][string]$HostKind='Catalog',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture='X64',[string]$Package,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/HoverCard.json')
$ErrorActionPreference='Stop'
$app=(Resolve-Path -LiteralPath $AppDirectory).Path
$target=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $target -Force | Out-Null
$report=Join-Path $target 'hover-cards.json'
if(Test-Path -LiteralPath $report){throw 'Use a fresh HoverCard output directory.'}
$hostName=if($HostKind -eq 'Catalog'){'Orchard.Catalog'}else{'PackageConsumer'}
$referencePath=(Resolve-Path -LiteralPath $Reference).Path
$identity=Join-Path $target 'app-identity';New-Item -ItemType Directory -Path $identity -Force | Out-Null
foreach($file in @('Orchard.dll',"$hostName.dll")){Copy-Item -LiteralPath (Join-Path $app $file) -Destination $identity}
$exe=Join-Path $app "$hostName.exe"
$process=Start-Process -FilePath $exe -ArgumentList "--verify-hidden-hover-cards `"$referencePath`" --verify-output `"$report`"" -PassThru -WindowStyle Hidden
@{processId=$process.Id;executable=$exe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $target 'process.json')
if(-not $process.WaitForExit(120000)){$process.Kill();throw 'Own hidden HoverCard run exceeded its watchdog.'}
@{exited=$true;exitCode=$process.ExitCode}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $target 'process-exit.json')
if($process.ExitCode -ne 0){throw "Native HoverCard failed; see $report"}
$audit=@((Join-Path $PSScriptRoot 'verify-hidden-hover-cards.mjs'),'--native',$target,'--app',$identity,'--host',$hostName,'--architecture',$ExpectedArchitecture,'--reference',$referencePath,'--output',(Join-Path $target 'hover-card-audit.json'))
if($HostKind -eq 'PackageConsumer'){if(-not $Package){throw 'The independent HoverCard consumer requires its exact package.'};$audit+=@('--package',(Resolve-Path -LiteralPath $Package).Path)}
node @audit
if($LASTEXITCODE -ne 0){throw 'Independent HoverCard audit failed.'}
