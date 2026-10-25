param([Parameter(Mandatory)][string]$Package,[ValidateSet('x64','ARM64')][string]$Platform='x64',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskRepository=Split-Path $PSScriptRoot -Parent
$taskPackage=(Resolve-Path -LiteralPath $Package).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh consumer output'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskZip=[IO.Compression.ZipFile]::OpenRead($taskPackage)
try{$taskNuspec=@($taskZip.Entries|Where-Object{$_.FullName -like '*.nuspec'});if($taskNuspec.Count -ne 1){throw 'Expected one package manifest'};$taskReader=[IO.StreamReader]::new($taskNuspec[0].Open());try{[xml]$taskManifest=$taskReader.ReadToEnd()}finally{$taskReader.Dispose()};$taskVersion=[string]$taskManifest.package.metadata.version}finally{$taskZip.Dispose()}
$taskSharedCache=(dotnet nuget locals global-packages --list) -replace '^global-packages:\s*',''
if($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $taskSharedCache -PathType Container)){throw 'The installed dependency cache is unavailable'}
$taskTemp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]'\/')
$taskRoot=Join-Path $taskTemp ('orchard-file-input-consumer-'+[Guid]::NewGuid().ToString('N'))
$taskSuccess=$false
try{
    foreach($taskFolder in @($taskRoot,(Join-Path $taskRoot 'source'),(Join-Path $taskRoot 'feed'))){New-Item -ItemType Directory -Path $taskFolder|Out-Null}
    Get-ChildItem -LiteralPath (Join-Path $taskRepository 'tests/PackageConsumer') -File|Copy-Item -Destination (Join-Path $taskRoot 'source')
    Copy-Item -LiteralPath (Join-Path $taskRepository 'global.json') -Destination $taskRoot
    Copy-Item -LiteralPath $taskPackage -Destination (Join-Path $taskRoot 'feed')
    $taskProject=Join-Path $taskRoot 'source/PackageConsumer.csproj'
    if((Get-Content -Raw -LiteralPath $taskProject) -match 'ProjectReference'){throw 'Independent consumer cannot reference repository projects'}
    $taskConfig=[xml]'<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear/><add key="orchard" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="orchard"><package pattern="Gaulatti.Orchard"/></packageSource><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping><fallbackPackageFolders><clear/><add key="installed-dependencies" value=""/></fallbackPackageFolders></configuration>'
    $taskConfig.configuration.packageSources.add[0].SetAttribute('value',(Join-Path $taskRoot 'feed'))
    $taskConfig.configuration.fallbackPackageFolders.add.SetAttribute('value',$taskSharedCache)
    $taskConfigPath=Join-Path $taskRoot 'NuGet.Config';$taskConfig.Save($taskConfigPath)
    $taskPackages=Join-Path $taskRoot 'packages'
    $taskRestored=Join-Path $taskPackages ('gaulatti.orchard/'+$taskVersion+'/gaulatti.orchard.'+$taskVersion+'.nupkg')
    if(Test-Path -LiteralPath $taskRestored){throw 'Fresh Orchard cache unexpectedly exists'}
    dotnet restore $taskProject --configfile $taskConfigPath --packages $taskPackages -p:Platform=$Platform -p:OrchardPackageVersion=$taskVersion *> (Join-Path $taskOutput 'restore.log')
    if($LASTEXITCODE -ne 0){throw 'Independent consumer restore failed'}
    if(-not (Test-Path -LiteralPath $taskRestored)){throw 'Orchard must be freshly restored into its isolated cache'}
    if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskPackage).Hash -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $taskRestored).Hash){throw 'Consumer restored different package bytes'}
    Copy-Item -LiteralPath $taskRestored -Destination (Join-Path $taskOutput 'restored-package.nupkg')
    Copy-Item -LiteralPath $taskConfigPath -Destination $taskOutput
    dotnet publish $taskProject -c Release -p:Platform=$Platform -p:OrchardPackageVersion=$taskVersion -p:UseSharedCompilation=false -m:1 -nr:false --no-restore -o (Join-Path $taskOutput 'app') *> (Join-Path $taskOutput 'publish.log')
    if($LASTEXITCODE -ne 0){throw 'Independent consumer publish failed'}
    $taskFiles=@(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'source') -File|ForEach-Object{@{file=$_.Name;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLower()}})
    [ordered]@{buildPassed=$true;runtimeVerified=$false;version=$taskVersion;platform=$Platform;packageSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskPackage).Hash.ToLower();isolatedProject=$taskRoot;freshOrchardCache=$true;thirdPartyDependenciesReused=$true;dependencyCache=$taskSharedCache;sourceFiles=$taskFiles;nativeComponentAccepted=$false}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $taskOutput 'result.json')
    $taskSuccess=$true
}finally{
    if($taskSuccess){
        $taskResolved=[IO.Path]::GetFullPath($taskRoot)
        if(-not $taskResolved.StartsWith($taskTemp+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($taskResolved) -notmatch '^orchard-file-input-consumer-[0-9a-f]{32}$'){throw 'Consumer cleanup target escaped its exact temporary root'}
        Remove-Item -LiteralPath $taskResolved -Recurse -Force
        @{removed=$true;path=$taskResolved;sharedDependencyCachePreserved=$true}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'cleanup.json')
    }else{@{removed=$false;path=$taskRoot;reason='Retained failed consumer for diagnosis'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'cleanup.json')}
}
