$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$temp = Join-Path ([IO.Path]::GetTempPath()) ('ndi-version-release-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp,(Join-Path $temp 'installer'),(Join-Path $temp 'companion-module-ndiplayer') | Out-Null
Push-Location
try {
    Copy-Item -LiteralPath (Join-Path $root 'build-release.ps1') -Destination $temp
    [IO.File]::WriteAllText((Join-Path $temp 'runtime.exe'), '')
    [IO.File]::WriteAllText((Join-Path $temp 'iscc.exe'), '')
    [IO.File]::WriteAllText((Join-Path $temp 'npm.cmd'), "@echo off`r`nexit /b 0`r`n")
    [IO.File]::WriteAllText((Join-Path $temp 'build.ps1'), @'
param([string]$VersionIncrement)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'increment.txt'), $VersionIncrement)
$global:LASTEXITCODE = 0
'@)
    [IO.File]::WriteAllText((Join-Path $temp 'installer\build-installer.ps1'), @'
param([string]$RuntimeFile,[string]$IsccPath,[string]$VideoArchive,[switch]$SkipBuild)
if (!$SkipBuild) { throw 'La release deve riutilizzare la build.' }
$global:LASTEXITCODE = 0
'@)
    $script = Join-Path $temp 'build-release.ps1'
    $runtime = Join-Path $temp 'runtime.exe'
    $iscc = Join-Path $temp 'iscc.exe'
    $npm = Join-Path $temp 'npm.cmd'
    foreach ($case in @(
        @{ Options = @(); Expected = 'patch' },
        @{ Options = @('--patch'); Expected = 'patch' },
        @{ Options = @('--minor'); Expected = 'minor' },
        @{ Options = @('--major'); Expected = 'major' }
    )) {
        $options = $case.Options
        & $script -RuntimeFile $runtime -IsccPath $iscc -NpmPath $npm @options
        $actual = [IO.File]::ReadAllText((Join-Path $temp 'increment.txt'))
        if ($actual -ne $case.Expected) { throw "Incremento atteso $($case.Expected), ottenuto $actual." }
    }
    & $script $runtime $iscc -NpmPath $npm --minor
    if ([IO.File]::ReadAllText((Join-Path $temp 'increment.txt')) -ne 'minor') { throw 'Argomenti posizionali o --minor non rispettati.' }
    & $script -RuntimeFile $runtime -IsccPath $iscc -NpmPath $npm -Major
    if ([IO.File]::ReadAllText((Join-Path $temp 'increment.txt')) -ne 'major') { throw 'Opzione PowerShell -Major non rispettata.' }
    foreach ($case in @(
        @{ Options = @('--major','--minor'); Error = 'Specificare una sola opzione' },
        @{ Options = @('--unknown'); Error = 'Opzione non riconosciuta' }
    )) {
        $failed = $false
        $options = $case.Options
        try { & $script -RuntimeFile $runtime -IsccPath $iscc -NpmPath $npm @options } catch {
            if ($_.Exception.Message -notlike "*$($case.Error)*") { throw }
            $failed = $true
        }
        if (!$failed) { throw "Opzioni non valide accettate: $($case.Options)" }
    }
    Write-Output 'PASS selezione incremento release e rifiuto opzioni non valide'
} finally {
    Pop-Location
    Remove-Item -LiteralPath $temp -Recurse -Force
}
