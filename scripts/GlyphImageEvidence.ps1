# Shared read-only PE image and symbol identity evidence.
function Get-ImageEvidence([string]$Path) {
    $file = Get-Item -LiteralPath $Path
    $stream = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    $reader = $null
    try {
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        $symbols = @($reader.ReadDebugDirectory() | Where-Object { $_.Type.ToString() -eq 'CodeView' } | ForEach-Object {
            $data = $reader.ReadCodeViewDebugDirectoryData($_)
            [ordered]@{ guid=$data.Guid.ToString('N'); age=$data.Age; path=$data.Path }
        })
        [ordered]@{
            path=$file.FullName; bytes=$file.Length
            sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            machine=$reader.PEHeaders.CoffHeader.Machine.ToString()
            fileVersion=$file.VersionInfo.FileVersion; productVersion=$file.VersionInfo.ProductVersion
            codeView=$symbols
        }
    } finally {
        if ($null -ne $reader) { $reader.Dispose() }
        $stream.Dispose()
    }
}
