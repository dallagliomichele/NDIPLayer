param(
    [string]$VersionIncrement = 'patch',
    [switch]$NoBump,
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$RemainingArguments
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$noBumpRequested = $NoBump
$noBumpWasVersionArgument = $VersionIncrement -eq '--no-bump'
if ($noBumpWasVersionArgument) {
    $noBumpRequested = $true
    $VersionIncrement = 'patch'
}
foreach ($argument in $RemainingArguments) {
    if ($argument -eq '--no-bump') {
        $noBumpRequested = $true
    } else {
        throw "Opzione non riconosciuta: $argument"
    }
}
if ($VersionIncrement -notin @('patch','minor','major')) {
    throw "Incremento versione non riconosciuto: $VersionIncrement"
}
if ($noBumpRequested -and $PSBoundParameters.ContainsKey('VersionIncrement') -and !$noBumpWasVersionArgument) {
    throw 'Non specificare VersionIncrement insieme a --no-bump.'
}
New-Item -ItemType Directory -Force bin | Out-Null
$lock = [IO.File]::Open((Join-Path $PSScriptRoot 'bin\version.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$id = [Guid]::NewGuid().ToString('N')
$assemblyInfo = Join-Path $PSScriptRoot "bin\AssemblyVersion-$id.cs"
$nextFile = Join-Path $PSScriptRoot "bin\VERSION-$id.tmp"
$backupFile = Join-Path $PSScriptRoot "bin\VERSION-$id.bak"
$buildDir = Join-Path $PSScriptRoot "bin\build-$id"
try {
$versionFile = Join-Path $PSScriptRoot 'VERSION'
$current = [IO.File]::ReadAllText($versionFile).Trim()
if ($current -notmatch '^(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})$') { throw 'VERSION deve contenere major.minor.patch numerici.' }
$major = [int]$Matches[1]; $minor = [int]$Matches[2]; $patch = [int]$Matches[3]
if ($major -gt 65534 -or $minor -gt 65534 -or $patch -gt 65534) { throw 'VERSION supera i limiti della versione assembly.' }
if (!$noBumpRequested) {
    switch ($VersionIncrement) {
        'major' { if ($major -ge 65534) { throw 'Versione major al limite della versione assembly.' }; $major++; $minor = 0; $patch = 0 }
        'minor' { if ($minor -ge 65534) { throw 'Versione minor al limite della versione assembly.' }; $minor++; $patch = 0 }
        'patch' { if ($patch -ge 65534) { throw 'Versione patch al limite della versione assembly.' }; $patch++ }
    }
}
$next = "$major.$minor.$patch"
$attributes = "[assembly: System.Reflection.AssemblyVersion(`"$next.0`")]`r`n[assembly: System.Reflection.AssemblyFileVersion(`"$next.0`")]`r`n[assembly: System.Reflection.AssemblyInformationalVersion(`"$next`")]`r`n"
[IO.File]::WriteAllText($assemblyInfo, $attributes, [Text.Encoding]::ASCII)
New-Item -ItemType Directory -Path $buildDir | Out-Null
$stagedApp = Join-Path $buildDir 'WatchoutImageNDI.exe'
if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'bin\NAudio.dll'))){& (Join-Path $PSScriptRoot 'installer\prepare-audio.ps1')}
if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'bin\SharpDX.Direct3D11.dll'))){& (Join-Path $PSScriptRoot 'installer\prepare-gpu.ps1')}
$gpuRefs = @('/reference:bin\SharpDX.dll','/reference:bin\SharpDX.DXGI.dll','/reference:bin\SharpDX.Direct3D11.dll','/reference:bin\SharpDX.D3DCompiler.dll')
if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'bin\video\ffmpeg.exe')) -or !(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'bin\video\ffprobe.exe'))){& (Join-Path $PSScriptRoot 'installer\prepare-video.ps1')}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$stagedApp" /reference:bin\NAudio.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll $gpuRefs $assemblyInfo src\AppVersion.cs src\GpuRenderer.cs src\ShellUi.cs src\WindowPlacement.cs src\AudioModel.cs src\AudioDecoder.cs src\AudioEngine.cs src\AudioDevices.cs src\AudioUi.cs src\CountdownSettings.cs src\CountdownUi.cs src\VideoClock.cs src\LibraryNavigation.cs src\Video.cs src\VideoUi.cs src\Core.cs src\Ndi.cs src\PreviewCanvas.cs src\LibraryBrowser.cs src\Projects.cs src\ProjectUi.cs src\PreviewOutput.cs src\EscapeGesture.cs src\CompanionControl.cs src\CompanionControlUi.cs src\CompanionTcp.cs src\CompanionUi.cs src\KeyboardRecall.cs src\ImageNumbers.cs src\LibraryActions.cs src\FolderUi.cs src\App.cs
if ($LASTEXITCODE -ne 0) { throw 'Build fallita' }
& $compiler /nologo /target:exe /platform:x64 /optimize+ /main:Tests /out:bin\Tests.exe /reference:bin\NAudio.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll $gpuRefs $assemblyInfo src\AppVersion.cs src\GpuRenderer.cs src\ShellUi.cs src\WindowPlacement.cs src\AudioModel.cs src\AudioDecoder.cs src\AudioEngine.cs src\AudioDevices.cs src\AudioUi.cs src\CountdownSettings.cs src\CountdownUi.cs src\VideoClock.cs src\LibraryNavigation.cs src\Video.cs src\VideoUi.cs src\Core.cs src\Ndi.cs src\PreviewCanvas.cs src\LibraryBrowser.cs src\Projects.cs src\ProjectUi.cs src\PreviewOutput.cs src\EscapeGesture.cs src\CompanionControl.cs src\CompanionControlUi.cs src\CompanionTcp.cs src\CompanionUi.cs src\KeyboardRecall.cs src\ImageNumbers.cs src\LibraryActions.cs src\FolderUi.cs src\App.cs tests\Tests.cs tests\FeatureTests.cs tests\TransformTests.cs tests\ProjectTests.cs tests\RoutingTests.cs tests\CompanionTests.cs tests\StopAndNumberTests.cs tests\RecallTests.cs tests\ThumbnailLifetimeTests.cs tests\VideoTests.cs tests\ControlAndCountdownTests.cs tests\CountdownPaintTests.cs tests\AudioTests.cs
if ($LASTEXITCODE -ne 0) { throw 'Build test fallita' }
& .\bin\Tests.exe
if ($LASTEXITCODE -ne 0) { throw 'Test falliti' }
Move-Item -LiteralPath $stagedApp -Destination (Join-Path $PSScriptRoot 'bin\WatchoutImageNDI.exe') -Force
if (!$noBumpRequested) {
    [IO.File]::WriteAllText($nextFile, "$next`n", [Text.Encoding]::ASCII)
    [IO.File]::Replace($nextFile, $versionFile, $backupFile)
}
Write-Host "Versione build: $next$(if ($noBumpRequested) { ' (senza bump)' })"
} finally {
 $lock.Dispose()
 if (Test-Path -LiteralPath $assemblyInfo) { Remove-Item -LiteralPath $assemblyInfo -Force }
 if (Test-Path -LiteralPath $nextFile) { Remove-Item -LiteralPath $nextFile -Force }
 if (Test-Path -LiteralPath $backupFile) { Remove-Item -LiteralPath $backupFile -Force }
 if (Test-Path -LiteralPath $buildDir) { Remove-Item -LiteralPath $buildDir -Recurse -Force }
}
