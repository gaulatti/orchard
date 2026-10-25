param(
    [Parameter(Mandatory)][string]$Output,
    [string]$Compiler='C:/Program Files (x86)/Windows Kits/10/bin/10.0.19041.0/x64/fxc.exe',
    [string]$Include='C:/Program Files (x86)/Windows Kits/10/Include/10.0.19041.0/um'
)
$ErrorActionPreference='Stop'
$taskRepository=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh shader output directory'}
if(-not (Test-Path -LiteralPath $Compiler -PathType Leaf) -or -not (Test-Path -LiteralPath (Join-Path $Include 'd2d1effecthelpers.hlsli') -PathType Leaf)){throw 'The documented Windows SDK FXC and Direct2D include are required'}
$taskShaders=Join-Path $taskRepository 'src/Orchard/Rendering/Shaders'
$taskManifestPath=Join-Path $taskRepository 'tests/Reference/OverlayNearOwnerShaders.json'
$taskManifest=Get-Content -LiteralPath $taskManifestPath -Raw|ConvertFrom-Json
if($taskManifest.format -ne 'orchard-overlay-near-shaders-v1'){throw 'Unexpected near shader manifest format'}
$taskNames=@($taskManifest.shaders|ForEach-Object{$_.name})
if($taskNames.Count -eq 0 -or @($taskNames|Sort-Object -Unique).Count -ne $taskNames.Count){throw 'Empty or duplicate near shader names'}
foreach($taskName in $taskNames){if($taskName -notmatch '^[a-z][a-z0-9-]*$'){throw 'Invalid near shader name'}}
$taskExpected=@($taskNames|Sort-Object)
foreach($taskExtension in @('.hlsl','.bin')){
    if(Compare-Object $taskExpected @(Get-ChildItem -LiteralPath $taskShaders -File -Filter ('*'+$taskExtension)|ForEach-Object{$_.BaseName}|Sort-Object)){throw 'Near shader source/binary inventory differs from the manifest'}
}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskRecords=@()
foreach($taskName in $taskNames){
    $taskSource=Join-Path $taskShaders "$taskName.hlsl"
    $taskSpec=$taskManifest.shaders|Where-Object name -eq $taskName
    if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskSource).Hash.ToLower() -ne $taskSpec.sourceSha256 -or (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $taskShaders "$taskName.bin")).Hash.ToLower() -ne $taskSpec.sha256){throw "Near shader manifest hash differs: $taskName"}
    $taskLibrary=Join-Path $taskOutput "$taskName.fxlib"
    $taskBinary=Join-Path $taskOutput "$taskName.bin"
    & $Compiler /nologo /T lib_4_0 /D D2D_FUNCTION /D D2D_ENTRY=main /I $Include /Fl $taskLibrary $taskSource *> (Join-Path $taskOutput "$taskName-library.log")
    if($LASTEXITCODE -ne 0){throw "Shader function compilation failed: $taskName"}
    & $Compiler /nologo /T ps_4_0 /D D2D_FULL_SHADER /D D2D_ENTRY=main /E main /I $Include /setprivate $taskLibrary /Fo $taskBinary $taskSource *> (Join-Path $taskOutput "$taskName-shader.log")
    if($LASTEXITCODE -ne 0){throw "Linked shader compilation failed: $taskName"}
    $taskRecords+=@{name=$taskName;sourceSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskSource).Hash.ToLower();compiledSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskBinary).Hash.ToLower();embeddedSourceSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $taskShaders "$taskName.bin")).Hash.ToLower()}
}
$taskExact=@($taskRecords|Where-Object{$_.compiledSha256 -ne $_.embeddedSourceSha256}).Count -eq 0
[ordered]@{passed=$taskExact;manifestSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskManifestPath).Hash.ToLower();manifestInventoryAndHashesVerified=$true;compiler=(Resolve-Path -LiteralPath $Compiler).Path;compilerVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo($Compiler).FileVersion;include=(Resolve-Path -LiteralPath $Include).Path;profiles=@('lib_4_0','ps_4_0');shaders=$taskRecords;embeddedBinariesByteExact=$taskExact;sourceBinariesChanged=$false}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $taskOutput 'compile.json')
if(-not $taskExact){throw 'Compiled output differs from embedded shader bytes; review the fresh outputs before updating the library resources'}
