[CmdletBinding(PositionalBinding=$false)]
param(
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
foreach ($argument in $RemainingArguments) {
    if ($argument -eq '--no-bump') {
        $noBumpRequested = $true
    } elseif ($argument -in @('--minor','--major','--patch')) {
        $selected += $argument.Substring(2).ToLowerInvariant()
    } else {
        throw "Opzione non riconosciuta: $argument"
    }
}
if ($selected.Count -gt 1) { throw 'Specificare una sola opzione tra --patch, --minor e --major.' }
if ($noBumpRequested -and $selected.Count -gt 0) { throw 'Non specificare --no-bump insieme a --patch, --minor o --major.' }
$increment = if ($selected.Count -eq 1) { $selected[0] } else { 'patch' }

$moduleDir = Join-Path $PSScriptRoot 'companion-module-ndiplayer'
Push-Location $moduleDir
try {
    & $NpmPath ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Installazione dipendenze Companion fallita' }
    & $NpmPath test
    if ($LASTEXITCODE -ne 0) { throw 'Test del modulo Companion falliti' }

    $versionScript = Join-Path $moduleDir 'scripts\version.js'
    if ($noBumpRequested) {
        & $NodePath $versionScript
    } else {
        & $NodePath $versionScript $increment
    }
    if ($LASTEXITCODE -ne 0) { throw 'Aggiornamento versione modulo Companion fallito' }

    & $NpmPath run package
    if ($LASTEXITCODE -ne 0) { throw 'Packaging del modulo Companion fallito' }
    & $NpmPath run check
    if ($LASTEXITCODE -ne 0) { throw 'Verifica del pacchetto Companion fallita' }
}
finally {
    Pop-Location
}
