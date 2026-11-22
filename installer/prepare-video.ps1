param([string]$Archive)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$cache=Join-Path $root 'bin\video-package'
$target=Join-Path $root 'bin\video'
New-Item -ItemType Directory -Force $cache,$target | Out-Null
$name='ffmpeg-n8.1.3-27-g8dca1586f4-win64-lgpl-shared-8.1.zip'
$expected='5CEF7BC48E513F3CEE7C881E8B2999D835541EDB1846C3E89443A67F8F8556AB'
$url='https://github.com/FFmpeg-Builds/FFmpeg-Builds/releases/download/autobuild-2026-10-31-12-59/'+$name
if(!$Archive){$Archive=Join-Path $cache $name;if(!(Test-Path -LiteralPath $Archive)){Invoke-WebRequest $url -OutFile $Archive}}
if((Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash -ne $expected){throw 'Archivio FFmpeg diverso dalla build verificata. Usare il pacchetto LGPL del 31 ottobre 2026 documentato in third-party.'}
$unpack=Join-Path $cache 'unpacked'
Expand-Archive -LiteralPath $Archive -DestinationPath $unpack -Force
$package=Join-Path $unpack 'ffmpeg-n8.1.3-27-g8dca1586f4-win64-lgpl-shared-8.1'
$source='8dca1586f468bfcbb78ae3ed618af1361d9f140a'
$builder='5031c6f1082414e53688d39e521849905071070d'
foreach($item in @(@('FFmpeg-source.zip',"FFmpeg-source-$source.zip","https://github.com/FFmpeg/FFmpeg/archive/$source.zip"),@('FFmpeg-build-scripts.zip',"FFmpeg-build-scripts-$builder.zip","https://github.com/FFmpeg-Builds/FFmpeg-Builds/archive/$builder.zip"),@('COPYING.GPLv3',"COPYING.GPLv3-$source","https://raw.githubusercontent.com/FFmpeg/FFmpeg/$source/COPYING.GPLv3"))){$file=Join-Path $cache $item[1];if(!(Test-Path -LiteralPath $file)){Invoke-WebRequest $item[2] -OutFile $file};Copy-Item -LiteralPath $file -Destination (Join-Path $target $item[0]) -Force}
Copy-Item (Join-Path $package 'bin\*.dll') $target -Force
Copy-Item (Join-Path $package 'bin\ffmpeg.exe') $target -Force
Copy-Item (Join-Path $package 'bin\ffprobe.exe') $target -Force
Copy-Item (Join-Path $package 'LICENSE.txt') (Join-Path $target 'COPYING.LGPLv3') -Force
& (Join-Path $target 'ffmpeg.exe') -version | Set-Content (Join-Path $target 'BUILD-CONFIGURATION.txt')
if($LASTEXITCODE -ne 0){throw 'Componente video non eseguibile'}
