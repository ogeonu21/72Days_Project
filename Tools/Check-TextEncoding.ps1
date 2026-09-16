# Read-only UTF-8 validation for repository-owned text files.
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskUtf8 = [Text.UTF8Encoding]::new($false, $true)
$taskExtensions = @('.cs','.md','.txt','.json','.xml','.yaml','.yml','.unity',
    '.prefab','.asset','.meta','.shader','.cginc','.gradle','.properties',
    '.asmdef','.asmref','.rsp','.ps1','.uxml','.uss','.csv','.html','.css',
    '.js','.sh','.bat','.cmd','.disabled','.config','.toml')
$taskFailures = [Collections.Generic.List[string]]::new()
$taskCount = 0
$taskFiles = & git -C $taskRoot -c core.quotepath=false ls-files --cached --others --exclude-standard
if ($LASTEXITCODE -ne 0) { throw 'Unable to enumerate repository files.' }
foreach ($taskFile in ($taskFiles | Sort-Object -Unique)) {
    if ([IO.Path]::GetExtension($taskFile).ToLowerInvariant() -notin $taskExtensions -and
        [IO.Path]::GetFileName($taskFile) -notin @('.editorconfig','.gitattributes','.gitignore')) { continue }
    $taskPath = Join-Path $taskRoot $taskFile
    if (-not (Test-Path -LiteralPath $taskPath -PathType Leaf)) { continue }
    $taskBytes = [IO.File]::ReadAllBytes($taskPath)
    $taskIsUtf16 = $taskBytes.Length -ge 2 -and (
        ($taskBytes[0] -eq 255 -and $taskBytes[1] -eq 254) -or
        ($taskBytes[0] -eq 254 -and $taskBytes[1] -eq 255))
    # Some Unity .asset files are binary, not YAML; never decode or rewrite them.
    if (($taskBytes -contains 0) -and -not $taskIsUtf16 -and
        [IO.Path]::GetExtension($taskFile) -eq '.asset') { continue }
    $taskCount++
    try {
        $taskText = $taskUtf8.GetString($taskBytes)
        if ($taskText.Contains([char]0xfffd) -or $taskText.Contains([char]0)) {
            $taskFailures.Add("Invalid text character: $taskFile")
        }
    } catch [Text.DecoderFallbackException] {
        $taskFailures.Add("Not UTF-8: $taskFile")
    }
}
if ($taskFailures.Count -gt 0) {
    $taskFailures | Write-Output
    throw "Encoding check failed: $($taskFailures.Count) of $taskCount files."
}
Write-Output "UTF-8 check passed: $taskCount text files, zero invalid sequences or replacement characters."
