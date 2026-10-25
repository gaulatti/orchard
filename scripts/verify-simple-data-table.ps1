param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskBuilds=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)(Orchard\.Catalog|Orchard\.sln|Orchard/Orchard\.csproj|PackageConsumer\.csproj)' })
if($taskBuilds.Count -gt 0){throw 'Wait for the Orchard build to exit before launching a Simple DataTable probe'}
if(-not [Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll"))).Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-simple-table')))){throw 'Built host does not recognize the hidden Simple DataTable command'}
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'simple-data-table.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Simple DataTable output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Table.json') -Destination (Join-Path $taskTarget 'source-reference.json')
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-simple-table --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(180000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Simple DataTable probe failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-simple-data-table.mjs') --native $taskTarget --reference (Join-Path $PSScriptRoot '../tests/Reference/Table.json') --frames (Join-Path $PSScriptRoot '../tests/Reference/Table') --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'simple-data-table-audit.json')
if($LASTEXITCODE -ne 0){throw 'Native simple DataTable audit failed'}
