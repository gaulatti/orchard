param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Archive,[Parameter(Mandatory)][string]$ExpandedDirectory,[Parameter(Mandatory)][string]$Output,[Parameter(Mandatory)][string]$Version)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(Test-Path -LiteralPath (Join-Path $taskApp 'publish') -PathType Container){throw 'Catalog input contains nested publish output. Use a clean publish directory, not the build directory.'}
$taskArchive=[IO.Path]::GetFullPath($Archive)
$taskExpanded=[IO.Path]::GetFullPath($ExpandedDirectory)
foreach($taskPath in @($taskArchive,$taskExpanded,[IO.Path]::GetFullPath($Output))){if(Test-Path -LiteralPath $taskPath){throw 'Use fresh catalog artifact paths'}}
foreach($taskRequired in @('Orchard.Catalog.exe','Orchard.Catalog.dll','Orchard.dll','resources.pri','AuthShellsFixture.xbf','CommandSpotlightsFixture.xbf')){if(-not(Test-Path -LiteralPath (Join-Path $taskApp $taskRequired) -PathType Leaf)){throw "Required catalog entry missing: $taskRequired"}}
[IO.Compression.ZipFile]::CreateFromDirectory($taskApp,$taskArchive,[IO.Compression.CompressionLevel]::Optimal,$false)
[IO.Compression.ZipFile]::ExtractToDirectory($taskArchive,$taskExpanded)
$taskFiles=@(Get-ChildItem -LiteralPath $taskApp -Recurse -File | ForEach-Object {
    $taskRelative=[IO.Path]::GetRelativePath($taskApp,$_.FullName)
    $taskCopy=Join-Path $taskExpanded $taskRelative
    $taskOriginalHash=(Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLower()
    if(-not(Test-Path -LiteralPath $taskCopy -PathType Leaf) -or (Get-FileHash -Algorithm SHA256 -LiteralPath $taskCopy).Hash.ToLower() -ne $taskOriginalHash){throw "Extracted catalog bytes differ: $taskRelative"}
    @{path=$taskRelative.Replace('\','/');bytes=$_.Length;sha256=$taskOriginalHash}
})
if(@(Get-ChildItem -LiteralPath $taskExpanded -Recurse -File).Count -ne $taskFiles.Count){throw 'Extracted file count differs'}
[ordered]@{format='orchard-catalog-artifact-v1';version=$Version;archive=$taskArchive;archiveSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskArchive).Hash.ToLower();expandedDirectory=$taskExpanded;selfContained=$true;fileCount=$taskFiles.Count;librarySha256=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $taskApp 'Orchard.dll')).Hash.ToLower();allExtractedBytesVerified=$true;nativeArchiveReplayVerified=$false;nativeArchiveBothFictionalDemosVerified=$false;files=$taskFiles;nativeComponentAccepted=$false}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $Output
Write-Output "Catalog archived and extracted: $($taskFiles.Count) files byte verified; native replay pending"
