param(
    [Parameter(Mandatory=$true)][string]$RuntimeFile,
    [Parameter(Mandatory=$true)][string]$IsccPath,
    [string]$VideoArchive,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$runtime = (Resolve-Path -LiteralPath $RuntimeFile).Path
$iscc = (Resolve-Path -LiteralPath $IsccPath).Path
$expected = '5972CCA54649039318FF5F167FD9940270CAFC09678B01E0C35695382270A144'
if ((Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash -ne $expected) { throw 'Hash runtime diverso dalla versione verificata 6.4.0.2' }
$signature = Get-AuthenticodeSignature -LiteralPath $runtime
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Vizrt AG') { throw 'Firma runtime NDI non valida' }
& (Join-Path $PSScriptRoot 'prepare-video.ps1') -Archive $VideoArchive
if (!$SkipBuild) {
    & (Join-Path $PSScriptRoot '..\build.ps1')
}
$app = Join-Path $PSScriptRoot '..\bin\WatchoutImageNDI.exe'
if (!(Test-Path -LiteralPath $app)) { throw 'Applicazione non compilata: eseguire build.ps1 prima di usare SkipBuild.' }
$builtVersion = [Reflection.AssemblyName]::GetAssemblyName((Resolve-Path -LiteralPath $app).Path).Version.ToString(3)
$currentVersion = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\VERSION')).Trim()
if ($builtVersion -ne $currentVersion) { throw "Versione dell'app ($builtVersion) diversa da VERSION ($currentVersion): ricompilare prima dell'installer." }
& $iscc "/DRuntimeFile=$runtime" "/DAppVersion=$builtVersion" (Join-Path $PSScriptRoot 'NDIPlayer.iss')
if ($LASTEXITCODE -ne 0) { throw 'Compilazione installer fallita' }
