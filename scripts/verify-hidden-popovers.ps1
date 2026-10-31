param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Catalog','PackageConsumer')][string]$HostKind='Catalog',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture='X64',[string]$Package,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/Popover.json')
$ErrorActionPreference='Stop'
$app=(Resolve-Path -LiteralPath $AppDirectory).Path
$target=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $target -Force | Out-Null
$report=Join-Path $target 'popovers.json'
if(Test-Path -LiteralPath $report){throw 'Use a fresh Popover output directory.'}
$hostName=if($HostKind -eq 'Catalog'){'Orchard.Catalog'}else{'PackageConsumer'}
$referencePath=(Resolve-Path -LiteralPath $Reference).Path
$identity=Join-Path $target 'app-identity';New-Item -ItemType Directory -Path $identity -Force | Out-Null
foreach($file in @('Orchard.dll',"$hostName.dll")){Copy-Item -LiteralPath (Join-Path $app $file) -Destination $identity}
if($HostKind -eq 'PackageConsumer'){
    $consumerRoot=Split-Path $app -Parent
    foreach($receipt in @('result.json','cleanup.json')){
        $receiptPath=Join-Path $consumerRoot $receipt
        if(-not(Test-Path -LiteralPath $receiptPath -PathType Leaf)){throw "Missing independent package consumer $receipt receipt."}
        Copy-Item -LiteralPath $receiptPath -Destination $target
    }
}
$process=Start-Process -FilePath (Join-Path $app "$hostName.exe") -ArgumentList "--verify-hidden-popovers `"$referencePath`" --verify-output `"$report`"" -PassThru -WindowStyle Hidden
@{processId=$process.Id;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $target 'process.json')
if(-not $process.WaitForExit(120000)){$process.Kill();throw 'Own hidden Popover run exceeded its watchdog.'}
@{exited=$true;exitCode=$process.ExitCode}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $target 'process-exit.json')
if($process.ExitCode -ne 0){throw "Native Popover failed; see $report"}
$audit=@((Join-Path $PSScriptRoot 'verify-hidden-popovers.mjs'),'--native',$target,'--app',$identity,'--host',$hostName,'--architecture',$ExpectedArchitecture,'--reference',$referencePath,'--output',(Join-Path $target 'popover-audit.json'))
if($HostKind -eq 'PackageConsumer'){if(-not $Package){throw 'The independent Popover consumer requires its exact package.'};$audit+=@('--package',(Resolve-Path -LiteralPath $Package).Path)}
node @audit
if($LASTEXITCODE -ne 0){throw 'Independent Popover audit failed.'}
