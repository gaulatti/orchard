param(
 [Parameter(Mandatory)][string]$AppDirectory,
 [Parameter(Mandatory)][string]$Output,
 [Parameter(Mandatory)][string]$NormalBackup,
 [Parameter(Mandatory)][string]$Baseline
)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskBackup=(Resolve-Path -LiteralPath $NormalBackup).Path
$taskNormal=Get-Content -LiteralPath (Join-Path $taskBackup 'identity.json') -Raw|ConvertFrom-Json
$taskNames=@('Orchard.dll','Orchard.Catalog.dll','Orchard.Catalog.pdb','Orchard.pdb','resources.pri','Orchard.Catalog.exe','Orchard.Catalog.deps.json','Orchard.Catalog.runtimeconfig.json')
if($taskNormal.files.Count -ne 8 -or @($taskNormal.files|Where-Object{$_.name -notin $taskNames}).Count){throw 'Expected the eight normal host outputs'}
foreach($taskFile in $taskNormal.files){if((Get-FileHash -LiteralPath (Join-Path $taskBackup $taskFile.name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskFile.sha256){throw 'Normal backup bytes changed'}}
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('Orchard.Catalog.exe','PackageConsumer.exe','XamlCompiler.exe','MSBuild.exe','dotnet.exe')}).Count){throw 'Wait for builds and native probes before collection'}
$taskSource=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../tests/PackageConsumer/DataTablePerformanceFixture.cs')).Path
$taskSourceHash=(Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash.ToLowerInvariant()
$taskTool=Join-Path $PSScriptRoot '../artifacts/diagnostics/dotnet-trace-v10/dotnet-trace.exe'
$taskReader=Join-Path $PSScriptRoot 'diagnostics/NativeCostTraceReader/bin/Release/net10.0/TraceReader.dll'
if(-not (Test-Path -LiteralPath $taskTool) -or -not (Test-Path -LiteralPath $taskReader)){throw 'Install the pinned dotnet-trace and build NativeCostTraceReader first; see docs/data-table.md'}
if(-not [Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp 'Orchard.Catalog.dll'))).Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('orchard-data-table-performance-diagnostic-v1')))){throw 'The supplied host is not a generated diagnostic build'}
$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh diagnostic output folder'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null
& compact.exe /C /I /Q /A $taskTarget|Out-Null
if($LASTEXITCODE -ne 0){throw 'Could not mark the generated trace directory for compression'}
if((Get-PSDrive -Name C).Free -lt (1GB+($taskNormal.files|Measure-Object -Property bytes -Sum).Sum)){throw 'Insufficient free capacity for raw collection and normal-host restoration'}
$taskProcess=$null
try{
 Copy-Item -LiteralPath (Join-Path $taskBackup 'Orchard.dll') -Destination (Join-Path $taskApp 'Orchard.dll') -Force
 New-Item -ItemType Directory -Path (Join-Path $taskTarget 'app-identity')|Out-Null
 foreach($taskName in @('Orchard.dll','Orchard.Catalog.dll','resources.pri','Microsoft.UI.Xaml.dll')){Copy-Item -LiteralPath (Join-Path $taskApp $taskName) -Destination (Join-Path (Join-Path $taskTarget 'app-identity') $taskName)}
 $taskReport=Join-Path $taskTarget 'data-table-performance.json'
 $taskProcess=Start-Process -FilePath (Join-Path $taskApp 'Orchard.Catalog.exe') -ArgumentList "--probe-hidden-data-table-performance --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
 @{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');diagnosticOnly=$true;normalTimingRun=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json') -Encoding utf8
 & $taskTool collect --process-id $taskProcess.Id --profile dotnet-sampled-thread-time --providers 'Microsoft-Windows-DotNETRuntime:0x1:5,Orchard-DataTable-NativeCost:0xffffffffffffffff:5' --buffersize 64 --duration 00:00:04:00 --output (Join-Path $taskTarget 'native-cost.nettrace') *> (Join-Path $taskTarget 'collector.log')
 $taskTraceExit=$LASTEXITCODE
 $taskDone=$taskProcess.WaitForExit(15000)
 if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit()}
 @{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json') -Encoding utf8
 if($taskTraceExit -ne 0 -or -not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Diagnostic collection failed; inspect retained outputs'}
 if((Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskSourceHash){throw 'Qualifying fixture source changed during collection'}
}
finally{
 if($null -ne $taskProcess -and -not $taskProcess.HasExited){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID remains live; normal outputs not overwritten'};$taskProcess.Kill();$taskProcess.WaitForExit()}
 foreach($taskFile in $taskNormal.files){Copy-Item -LiteralPath (Join-Path $taskBackup $taskFile.name) -Destination (Join-Path $taskApp $taskFile.name) -Force;if((Get-FileHash -LiteralPath (Join-Path $taskApp $taskFile.name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskFile.sha256){throw 'Normal output restoration failed'}}
 @{passed=$true;recordedAt=[DateTime]::UtcNow.ToString('o');normalOutputRestoreRequired=$false;filesRestored=$taskNormal.files}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $taskTarget 'normal-output-restored.json') -Encoding utf8
}
dotnet $taskReader (Join-Path $taskTarget 'native-cost.nettrace') (Join-Path $taskTarget 'trace-events.json') *> (Join-Path $taskTarget 'reader.log')
if($LASTEXITCODE -ne 0){throw 'EventPipe decoding failed after normal outputs were restored'}
$taskRaw=(Get-Item -LiteralPath (Join-Path $taskTarget 'native-cost.nettrace')).Length
if((Get-PSDrive -Name C).Free -lt [Math]::Max([long](512MB),[long]($taskRaw*4))){throw 'Raw trace retained and normal outputs restored; conversion needs more free scratch capacity'}
& $taskTool convert (Join-Path $taskTarget 'native-cost.nettrace') --format Speedscope *> (Join-Path $taskTarget 'converter.log')
if($LASTEXITCODE -ne 0){throw 'Trace conversion failed after normal outputs were restored'}
node (Join-Path $PSScriptRoot 'analyze-data-table-native-cost.mjs') --native $taskTarget --baseline $Baseline --output (Join-Path $taskTarget 'analysis.json') *> (Join-Path $taskTarget 'analysis.log')
if($LASTEXITCODE -ne 0){throw 'Diagnostic semantic/phase analysis failed'}
