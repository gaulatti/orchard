param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$ImmutableManifest,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskImmutableRoot=[IO.Path]::GetFullPath('artifacts/current')+[IO.Path]::DirectorySeparatorChar
if(-not $taskApp.StartsWith($taskImmutableRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Chart identity links require a frozen host under artifacts/current'}
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use fresh chart-consumer evidence'}
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i) (build|publish|restore) ')}).Count){throw 'Wait for SDK operations before native chart checks'}
$taskManifest=Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json
if(-not $taskManifest.passed -or $taskManifest.mutableOutputHardlinked -ne $false){throw 'Chart manifest must identify verified immutable host files'}
New-Item -ItemType Directory -Path (Join-Path $taskOutput 'app-identity')|Out-Null
foreach($taskLeaf in @('Orchard.dll',($HostName+'.dll'),'Microsoft.UI.Xaml.dll','resources.pri')){
 $taskFrom=Join-Path $taskApp $taskLeaf;$taskEntry=@($taskManifest.files|Where-Object file -eq $taskLeaf)
 if($taskEntry.Count -ne 1 -or (Get-FileHash -LiteralPath $taskFrom).Hash.ToLowerInvariant() -ne $taskEntry[0].sha256){throw 'Frozen chart host differs from its manifest'}
 New-Item -ItemType HardLink -Path (Join-Path $taskOutput ('app-identity/'+$taskLeaf)) -Target $taskFrom|Out-Null
}
$taskReport=Join-Path $taskOutput 'chart-theme-consumers.json'
$taskProcess=Start-Process -FilePath (Join-Path $taskApp ($HostName+'.exe')) -ArgumentList "--probe-hidden-chart-theme-consumers --verify-output `"$taskReport`"" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;observationExpired=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
if($taskProcess.ExitCode -ne 0){throw 'Native chart-consumer checks failed; inspect retained report'}
& node (Join-Path $PSScriptRoot 'verify-chart-theme-consumers.mjs') --native $taskOutput --app $taskApp --host $HostName --output (Join-Path $taskOutput 'audit.json')
if($LASTEXITCODE -ne 0){throw 'Chart-consumer audit failed'}
