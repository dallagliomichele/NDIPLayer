[CmdletBinding(PositionalBinding=$false)]
param(
    [string]$RuntimeFile,
    [string]$IsccPath,
    [string]$VideoArchive,
    [string]$NodePath = 'node',
    [string]$NpmPath = 'npm',
    [switch]$Minor,
    [switch]$Major,
    [switch]$Patch,
    [switch]$NoBump,
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$RemainingArguments
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$selected = @()
if ($Minor) { $selected += 'minor' }
if ($Major) { $selected += 'major' }
if ($Patch) { $selected += 'patch' }
$noBumpRequested = $NoBump
$positionals = @('RuntimeFile','IsccPath','VideoArchive','NodePath','NpmPath')
$position = 0
foreach ($argument in $RemainingArguments) {
    if ($argument -eq '--no-bump') {
        $noBumpRequested = $true
    } elseif ($argument -in @('--minor','--major','--patch')) {
        $selected += $argument.Substring(2).ToLowerInvariant()
    } elseif ($argument.StartsWith('-')) {
        throw "Opzione non riconosciuta: $argument"
    } else {
        while ($position -lt $positionals.Count -and $PSBoundParameters.ContainsKey($positionals[$position])) { $position++ }
        if ($position -ge $positionals.Count) { throw "Argomento inatteso: $argument" }
        Set-Variable -Name $positionals[$position] -Value $argument
        $position++
    }
}
if ($selected.Count -gt 1) { throw 'Specificare una sola opzione tra --patch, --minor e --major.' }
if ($noBumpRequested -and $selected.Count -gt 0) { throw 'Non specificare --no-bump insieme a --patch, --minor o --major.' }
$increment = if ($selected.Count -eq 1) { $selected[0] } else { 'patch' }

if (!$RuntimeFile) {
    $RuntimeFile = Join-Path $PSScriptRoot 'dependencies\NDI6Runtime.exe'
}
if (!$IsccPath) {
    $isccCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles} 'Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles} 'Inno Setup 7\ISCC.exe')
    )
    $IsccPath = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!(Test-Path -LiteralPath $RuntimeFile)) {
    throw "Runtime NDI non trovato: $RuntimeFile. Eseguire install-dependencies.ps1."
}
if (!$IsccPath) {
    throw 'Inno Setup non trovato. Eseguire install-dependencies.ps1 -InstallInnoSetup.'
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory=$true)][string]$Command,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [Parameter(Mandatory=$true)][string]$FailureMessage
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

Write-Host 'Compilazione applicazione e test...'
if ($VideoArchive) {
    & (Join-Path $PSScriptRoot 'installer\prepare-video.ps1') -Archive $VideoArchive
    if ($LASTEXITCODE -ne 0) {
        throw 'Preparazione archivio FFmpeg fallita'
    }
}
$buildScript = Join-Path $PSScriptRoot 'build.ps1'
if ($noBumpRequested) {
    & $buildScript --no-bump
} else {
    & $buildScript -VersionIncrement $increment
}
if ($LASTEXITCODE -ne 0) {
    throw 'Compilazione applicazione fallita'
}

$moduleDir = Join-Path $PSScriptRoot 'companion-module-ndiplayer'
Push-Location $moduleDir
try {
    Write-Host 'Installazione dipendenze del modulo Companion...'
    Invoke-Checked $NpmPath @('ci', '--no-audit', '--no-fund') 'Installazione dipendenze Companion fallita'
    Invoke-Checked $NpmPath @('test') 'Test del modulo Companion falliti'
    Invoke-Checked $NpmPath @('run', 'package') 'Packaging del modulo Companion fallito'
    Invoke-Checked $NpmPath @('run', 'check') 'Verifica del modulo Companion fallita'
}
finally {
    Pop-Location
}

$installerArgs = @{
    RuntimeFile = $RuntimeFile
    IsccPath = $IsccPath
    SkipBuild = $true
}
if ($VideoArchive) {
    $installerArgs.VideoArchive = $VideoArchive
}

Write-Host 'Preparazione dipendenze e creazione installer...'
& (Join-Path $PSScriptRoot 'installer\build-installer.ps1') @installerArgs
if ($LASTEXITCODE -ne 0) {
    throw 'Creazione installer fallita'
}

Write-Host 'Build completa.'
