param(
    [string]$RuntimeFile,
    [string]$IsccPath,
    [string]$VideoArchive,
    [string]$NodePath = 'node',
    [string]$NpmPath = 'npm'
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

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
& (Join-Path $PSScriptRoot 'build.ps1')
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
