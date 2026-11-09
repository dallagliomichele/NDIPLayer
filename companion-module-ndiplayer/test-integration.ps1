param([string]$NodePath='node')
$ErrorActionPreference='Stop'
$moduleDir=$PSScriptRoot
$repo=Split-Path $moduleDir -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$application=Join-Path $repo 'bin\WatchoutImageNDI.exe'
$hostExe=Join-Path $repo 'bin\ModuleIntegrationHost.exe'
if(!(Test-Path -LiteralPath $application)){throw 'Prima compilare NDIPlayer con build.ps1'}
& $compiler /nologo /target:exe /platform:x64 "/out:$hostExe" "/reference:$application" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll (Join-Path $moduleDir 'tests\fixtures\ModuleIntegrationHost.cs')
if($LASTEXITCODE -ne 0){throw 'Fixture non compilata'}
$previousHost=$env:NDIPLAYER_TEST_HOST
try{$env:NDIPLAYER_TEST_HOST=$hostExe;Push-Location $moduleDir;try{& $NodePath --test tests/module.test.js tests/integration.test.js;if($LASTEXITCODE -ne 0){throw 'Test modulo falliti'}}finally{Pop-Location}}finally{$env:NDIPLAYER_TEST_HOST=$previousHost}
