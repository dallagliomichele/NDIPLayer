$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force bin | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /out:bin\WatchoutImageNDI.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll src\Core.cs src\Ndi.cs src\PreviewCanvas.cs src\LibraryBrowser.cs src\Projects.cs src\ProjectUi.cs src\PreviewOutput.cs src\EscapeGesture.cs src\CompanionTcp.cs src\CompanionUi.cs src\ImageNumbers.cs src\LibraryActions.cs src\App.cs
if ($LASTEXITCODE -ne 0) { throw 'Build fallita' }
& $compiler /nologo /target:exe /platform:x64 /optimize+ /main:Tests /out:bin\Tests.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll src\Core.cs src\Ndi.cs src\PreviewCanvas.cs src\LibraryBrowser.cs src\Projects.cs src\ProjectUi.cs src\PreviewOutput.cs src\EscapeGesture.cs src\CompanionTcp.cs src\CompanionUi.cs src\ImageNumbers.cs src\LibraryActions.cs src\App.cs tests\Tests.cs tests\FeatureTests.cs tests\TransformTests.cs tests\ProjectTests.cs tests\RoutingTests.cs tests\CompanionTests.cs tests\StopAndNumberTests.cs
if ($LASTEXITCODE -ne 0) { throw 'Build test fallita' }
& .\bin\Tests.exe
if ($LASTEXITCODE -ne 0) { throw 'Test falliti' }
