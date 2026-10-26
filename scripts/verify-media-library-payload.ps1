param([Parameter(Mandatory)][string]$Package,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskRepository=Split-Path $PSScriptRoot -Parent
$taskPackage=(Resolve-Path -LiteralPath $Package).Path
if(Test-Path -LiteralPath $Output){throw 'Use a fresh payload report'}
$taskZip=[IO.Compression.ZipFile]::OpenRead($taskPackage)
try{
    $taskEntries=@($taskZip.Entries|Where-Object{$_.Name})
    $taskNames=@($taskEntries|ForEach-Object FullName)
    if($taskNames|Where-Object{$_ -match 'Catalog|Tests|\.cs$|\.exe$'}){throw 'Development infrastructure leaked into the library package'}
    $taskLibrary=@($taskEntries|Where-Object{$_.FullName -match '^lib/[^/]+/Orchard.dll$'})
    $taskThemes=@($taskEntries|Where-Object{$_.FullName -match '/Orchard/Themes/.+\.xaml$'})
    $taskXaml=@($taskEntries|Where-Object{$_.FullName -like '*.xaml'})
    $taskPri=@($taskEntries|Where-Object{$_.FullName -match '^lib/[^/]+/Orchard.pri$'})
    if($taskLibrary.Count -ne 1 -or $taskPri.Count -ne 1){throw 'Unexpected library/PRI payload counts'}
    $taskSourceXaml=@(git -C $taskRepository ls-files -- 'src/Orchard/*.xaml' 'src/Orchard/**/*.xaml')
    if($LASTEXITCODE -ne 0 -or $taskSourceXaml.Count -eq 0){throw 'Tracked library XAML inventory is unavailable'}
    $taskPrefix=$taskLibrary[0].FullName -replace 'Orchard.dll$','Orchard/'
    $taskExpectedXaml=@($taskSourceXaml|ForEach-Object{$taskPrefix+$_.Substring('src/Orchard/'.Length)})
    $taskActualXaml=@($taskXaml|ForEach-Object FullName)
    if($taskActualXaml.Count -ne @($taskActualXaml|Select-Object -Unique).Count -or
       @(Compare-Object -ReferenceObject $taskExpectedXaml -DifferenceObject $taskActualXaml -CaseSensitive).Count -ne 0){throw 'Packaged XAML paths differ from tracked library sources'}
    $taskSourceThemeCount=@($taskSourceXaml|Where-Object{$_ -like 'src/Orchard/Themes/*'}).Count
    if($taskThemes.Count -ne $taskSourceThemeCount){throw 'Packaged theme count differs from tracked sources'}
    $taskSourceHashes=@()
    foreach($taskEntry in $taskXaml){$taskRelative=$taskEntry.FullName -replace '^lib/[^/]+/Orchard/','';$taskSource=Join-Path (Join-Path $taskRepository 'src/Orchard') $taskRelative;$taskStream=$taskEntry.Open();try{$taskHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($taskStream)).ToLower()}finally{$taskStream.Dispose()};if($taskHash -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $taskSource).Hash.ToLower()){throw "Packaged XAML differs: $taskRelative"};$taskSourceHashes+=@{entry=$taskEntry.FullName;sha256=$taskHash}}
    foreach($taskRequired in @('README.md','buildTransitive/Gaulatti.Orchard.targets','licenses/LICENSE-Skia.txt','assets/fonts/EncodeSans-Regular.ttf','assets/fonts/EncodeSans-Medium.ttf','assets/fonts/EncodeSans-SemiBold.ttf','assets/fonts/EncodeSans-Bold.ttf','assets/fonts/LibreFranklin-Regular.ttf','assets/fonts/LibreFranklin-SemiBold.ttf','assets/images/gaulatti-logo.svg','assets/images/BleeckerLogo.png')){if($taskRequired -notin $taskNames){throw "Required package entry absent: $taskRequired"}}
    if(@($taskNames|Where-Object{$_ -like '*.ttf'}).Count -ne 6 -or @($taskNames|Where-Object{$_ -match '^assets/images/'}).Count -ne 2){throw 'Unexpected font/image payload counts'}
    $taskReader=[IO.StreamReader]::new(($taskEntries|Where-Object FullName -eq 'README.md').Open());try{$taskReadme=$taskReader.ReadToEnd()}finally{$taskReader.Dispose()}
    if($taskReadme -ne (Get-Content -Raw -LiteralPath (Join-Path $taskRepository 'docs/package-readme.md'))){throw 'Packaged README is stale'}
    if($taskReadme -notmatch 'BleeckerMediaLibrary'){throw 'Media Library package documentation is absent'}
    $taskMemory=[IO.MemoryStream]::new();$taskStream=$taskLibrary[0].Open();try{$taskStream.CopyTo($taskMemory)}finally{$taskStream.Dispose()};$taskBytes=$taskMemory.ToArray();$taskMemory.Position=0
    $taskPe=[Reflection.PortableExecutable.PEReader]::new($taskMemory)
    try{if($taskPe.PEHeaders.CoffHeader.Machine -ne [Reflection.PortableExecutable.Machine]::I386 -or ($taskPe.PEHeaders.CorHeader.Flags -band [Reflection.PortableExecutable.CorFlags]::Requires32Bit) -ne 0 -or ($taskPe.PEHeaders.CorHeader.Flags -band [Reflection.PortableExecutable.CorFlags]::ILOnly) -eq 0){throw 'Orchard must remain AnyCPU IL'}}finally{$taskPe.Dispose();$taskMemory.Dispose()}
    [ordered]@{passed=$true;package=$Package;packageSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskPackage).Hash.ToLower();librarySha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($taskBytes)).ToLower();anyCpu=$true;themes=$taskThemes.Count;looseXamlFiles=$taskXaml.Count;priFiles=1;fonts=6;images=2;readmeMatchesCurrentSource=$true;mediaLibraryThemePresent=(@($taskNames|Where-Object{$_ -match '/MediaLibrary.xaml$'}).Count -eq 1);xamlHashes=$taskSourceHashes;nativeComponentAccepted=$false}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $Output
}finally{$taskZip.Dispose()}
