param([string]$Archive)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$vendor=Join-Path $root 'third-party\binaries\NAudio.dll'
$target=Join-Path $root 'bin\NAudio.dll'
if(Test-Path -LiteralPath $vendor){
    New-Item -ItemType Directory -Force (Join-Path $root 'bin') | Out-Null
    Copy-Item -LiteralPath $vendor -Destination $target -Force
    return
}
$cache=Join-Path $root 'bin\audio-package'
New-Item -ItemType Directory -Force $cache | Out-Null
if(!$Archive){$Archive=Join-Path $cache 'naudio.1.10.0.zip';if(!(Test-Path -LiteralPath $Archive)){Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/naudio/1.10.0/naudio.1.10.0.nupkg' -OutFile $Archive}}
$expected='E4A80EED41DAD794695F9C114432DE4667D20BDFA80825AA94D159708CF29217'
if((Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash -ne $expected){throw 'Pacchetto NAudio diverso dalla versione verificata'}
$unpack=Join-Path $cache 'unpacked'
Expand-Archive -LiteralPath $Archive -DestinationPath $unpack -Force
Copy-Item (Join-Path $unpack 'lib\net35\NAudio.dll') $target -Force
