param(
    [switch]$InstallInnoSetup
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$dependencyDir = Join-Path $PSScriptRoot 'dependencies'
New-Item -ItemType Directory -Force $dependencyDir | Out-Null

$runtimeFile = Join-Path $dependencyDir 'NDI6Runtime.exe'
$runtimeUrl = 'https://downloads.ndi.tv/SDK/NDI_SDK/NDI%206%20Runtime.exe'
$runtimeHash = '5972CCA54649039318FF5F167FD9940270CAFC09678B01E0C35695382270A144'
if (!(Test-Path -LiteralPath $runtimeFile)) {
    Invoke-WebRequest -Uri $runtimeUrl -OutFile $runtimeFile
}
if ((Get-FileHash -LiteralPath $runtimeFile -Algorithm SHA256).Hash -ne $runtimeHash) {
    throw 'Runtime NDI diverso dalla versione verificata 6.4.0.2'
}
$runtimeSignature = Get-AuthenticodeSignature -LiteralPath $runtimeFile
if ($runtimeSignature.Status -ne 'Valid' -or $runtimeSignature.SignerCertificate.Subject -notmatch 'O=Vizrt AG') {
    throw 'Firma del runtime NDI non valida'
}

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) {
    throw '.NET Framework 4.8 con csc.exe non trovato'
}
if (!(Get-Command node -ErrorAction SilentlyContinue)) {
    throw 'Node.js 26 non trovato. Installarlo dal sito ufficiale Node.js e ripetere il comando.'
}

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles} 'Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
    (Join-Path ${env:ProgramFiles} 'Inno Setup 7\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (!$iscc -and $InstallInnoSetup) {
    $innoInstaller = Join-Path $dependencyDir 'innosetup-7.1.1-x64.exe'
    if (!(Test-Path -LiteralPath $innoInstaller)) {
        Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_1/innosetup-7.1.1-x64.exe' -OutFile $innoInstaller
    }
    $innoSignature = Get-AuthenticodeSignature -LiteralPath $innoInstaller
    if ($innoSignature.Status -ne 'Valid' -or $innoSignature.SignerCertificate.Subject -notmatch 'O=Pyrsys B.V.') {
        throw 'Firma dell installer Inno Setup non valida'
    }
    Start-Process -FilePath $innoInstaller -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -Wait
    $iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$iscc) {
    Write-Host 'Inno Setup non installato. Ripetere con -InstallInnoSetup.'
} else {
    Write-Host "Inno Setup trovato: $iscc"
}

Write-Host "Runtime NDI pronto: $runtimeFile"
Write-Host 'Dipendenze verificate. Eseguire ./build-release.ps1 per compilare.'
