param(
    [Parameter(Mandatory=$true)][string]$RuntimeFile,
    [Parameter(Mandatory=$true)][string]$IsccPath,
    [string]$VideoArchive
)
$ErrorActionPreference = 'Stop'
$runtime = (Resolve-Path -LiteralPath $RuntimeFile).Path
$iscc = (Resolve-Path -LiteralPath $IsccPath).Path
$expected = '7EE73EEDB56402BCA5100868353DBAB4E944B6C37D2D9881580698A3B61346CD'
if ((Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash -ne $expected) { throw 'Hash runtime diverso dalla versione verificata 6.3.2.0' }
$signature = Get-AuthenticodeSignature -LiteralPath $runtime
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Vizrt AG') { throw 'Firma runtime NDI non valida' }
if(!(Test-Path (Join-Path $PSScriptRoot '..\bin\video\ffmpeg.exe'))){& (Join-Path $PSScriptRoot 'prepare-video.ps1') -Archive $VideoArchive}
& (Join-Path $PSScriptRoot '..\build.ps1')
& $iscc "/DRuntimeFile=$runtime" (Join-Path $PSScriptRoot 'NDIPlayer.iss')
if ($LASTEXITCODE -ne 0) { throw 'Compilazione installer fallita' }
