param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskBuilds=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)(Orchard\.Catalog|Orchard\.sln|Orchard/Orchard\.csproj|PackageConsumer\.csproj)' })
if($taskBuilds.Count -gt 0){throw 'Wait for the Orchard build to exit before launching a Data Table probe'}
if(-not [Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll"))).Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-data-table-filter-plan')))){throw 'Built host does not recognize the hidden Data Table command'}
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'data-table-filter-plan.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Data Table output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/DataTableFilterPlan.json') -Destination (Join-Path $taskTarget 'filter-source-reference.json')
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-data-table-filter-plan --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(180000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Data Table probe failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-data-table-filter-plan.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'data-table-filter-plan-audit.json')
if($LASTEXITCODE -ne 0){throw 'Native Data Table audit failed'}

