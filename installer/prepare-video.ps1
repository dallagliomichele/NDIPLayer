param([string]$Archive)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$cache=Join-Path $root 'bin\video-package'
$target=Join-Path $root 'bin\video'
New-Item -ItemType Directory -Force $cache,$target | Out-Null
$name='ffmpeg-n8.1-latest-win64-lgpl-shared-8.1.zip'
$expected='7F2CD74C2805437D7F07739FDD7BEA7C9D851EE2DBA9503FC8E7D60C24A57447'
$url='https://github.com/FFmpeg-Builds/FFmpeg-Builds/releases/download/autobuild-2026-11-09-12-42/'+$name
if(!$Archive){$Archive=Join-Path $cache $name;if(!(Test-Path -LiteralPath $Archive)){Invoke-WebRequest $url -OutFile $Archive}}
if((Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash -ne $expected){throw 'Archivio FFmpeg diverso dalla build verificata. Usare il pacchetto LGPL del 9 novembre 2026 documentato in third-party.'}
$unpack=Join-Path $cache 'unpacked'
Expand-Archive -LiteralPath $Archive -DestinationPath $unpack -Force
$package=Join-Path $unpack 'ffmpeg-n8.1-latest-win64-lgpl-shared-8.1'
Copy-Item (Join-Path $package 'bin\*.dll') $target -Force
Copy-Item (Join-Path $package 'bin\ffmpeg.exe') $target -Force
Copy-Item (Join-Path $package 'LICENSE.txt') (Join-Path $target 'COPYING.LGPLv3') -Force
$source='a35157abb96167fd7308edc92b55408d5bb084b0'
$builder='c9fe5114060e507409f7ee07edfd82efaf25d767'
foreach($item in @(@('FFmpeg-source.zip',"https://github.com/FFmpeg/FFmpeg/archive/$source.zip"),@('FFmpeg-build-scripts.zip',"https://github.com/FFmpeg-Builds/FFmpeg-Builds/archive/$builder.zip"),@('COPYING.GPLv3',"https://raw.githubusercontent.com/FFmpeg/FFmpeg/$source/COPYING.GPLv3"))){$file=Join-Path $target $item[0];if(!(Test-Path -LiteralPath $file)){Invoke-WebRequest $item[1] -OutFile $file}}
& (Join-Path $target 'ffmpeg.exe') -version | Set-Content (Join-Path $target 'BUILD-CONFIGURATION.txt')
if($LASTEXITCODE -ne 0){throw 'Componente video non eseguibile'}
