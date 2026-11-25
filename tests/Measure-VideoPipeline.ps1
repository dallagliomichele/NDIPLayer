# With -DiscardFrames, FFmpeg writes rawvideo to NUL and the Hap worker
# discards pixels after readback. The default measures the BGRA pipe and
# saves first-frame samples for comparison with the software paths.
param(
    [string]$Ffmpeg,
    [ValidateRange(16, 4096)][int]$Width = 1920,
    [ValidateRange(16, 4096)][int]$Height = 1080,
    [ValidateRange(1, 120)][int]$Fps = 30,
    [ValidateRange(1, 60)][int]$Seconds = 4,
    [ValidateRange(1, 10)][int]$Repeats = 3,
    [ValidateSet('Motion', 'Bars')][string]$Pattern = 'Motion',
    [ValidateSet('none', 'snappy')][string]$HapCompressor = 'none',
    [ValidateRange(1, 64)][int]$HapChunks = 1,
    [switch]$DiscardFrames,
    [string]$OutputRoot = (Join-Path $PSScriptRoot '../bin/video-bench')
)
$ErrorActionPreference = 'Stop'
if ($Width % 4 -or $Height % 4) { throw 'Hap richiede larghezza e altezza multiple di 4.' }
if (!$Ffmpeg) {
    $Ffmpeg = Join-Path $PSScriptRoot '../bin/video/ffmpeg.exe'
    if (!(Test-Path -LiteralPath $Ffmpeg)) { $Ffmpeg = 'ffmpeg.exe' }
}
if (Test-Path -LiteralPath $Ffmpeg -PathType Leaf) {
    $Ffmpeg = (Resolve-Path -LiteralPath $Ffmpeg).ProviderPath
} else {
    $Ffmpeg = (Get-Command $Ffmpeg -CommandType Application).Source
}
$mode = if ($DiscardFrames) { 'discard' } else { 'pipe' }
$run = Join-Path $OutputRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Pattern.ToLowerInvariant() + "-$mode-$HapCompressor-$HapChunks")
$run = (New-Item -ItemType Directory -Path $run).FullName

function Invoke-VideoTool([string[]]$Arguments, [int]$FrameBytes = 0, [string]$Executable = $Ffmpeg, [string]$FirstFramePath) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $Executable
    $info.Arguments = ($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $process = [Diagnostics.Process]::Start($info)
    try {
        $stderr = $process.StandardError.ReadToEndAsync()
        $stream = $process.StandardOutput.BaseStream
        $buffer = New-Object byte[] ([Math]::Max(65536, $FrameBytes))
        $bytes = 0L
        $first = $null
        $firstPixels = $null
        $offset = 0
        while (($read = $stream.Read($buffer, $offset, $buffer.Length - $offset)) -gt 0) {
            $bytes += $read
            $offset = ($offset + $read) % $buffer.Length
            if ($FrameBytes -and $null -eq $first -and $bytes -ge $FrameBytes) {
                $first = $clock.Elapsed.TotalMilliseconds
                if ($FirstFramePath) { $firstPixels = $buffer.Clone() }
            }
        }
        $process.WaitForExit()
        $clock.Stop()
        if ($null -ne $firstPixels) { [IO.File]::WriteAllBytes($FirstFramePath, $firstPixels) }
        [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stderr = $stderr.GetAwaiter().GetResult()
            Bytes = $bytes
            FirstFrameMs = $first
            WallSeconds = $clock.Elapsed.TotalSeconds
        }
    } finally {
        $process.Dispose()
    }
}

$version = & $Ffmpeg -version
if ($LASTEXITCODE -ne 0) { throw "FFmpeg non eseguibile: $Ffmpeg" }
$version | Set-Content (Join-Path $run 'ffmpeg-version.txt')
$hapBench = Join-Path $run 'HapGpuBench.exe'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$refs = foreach ($name in 'SharpDX','SharpDX.DXGI','SharpDX.Direct3D11','SharpDX.D3DCompiler') {
    Copy-Item (Join-Path $PSScriptRoot "../third-party/binaries/$name.dll") $run
    "/reference:$(Join-Path $run "$name.dll")"
}
& $compiler /nologo /optimize+ /platform:x64 "/out:$hapBench" $refs (Join-Path $PSScriptRoot 'HapGpuBench.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilazione del banco Hap GPU fallita.' }
$generator = @{ Motion = 'testsrc2'; Bars = 'smptehdbars' }[$Pattern]
$fixtures = @(
    @{ Name = 'h264'; Encode = @('-c:v','libopenh264','-b:v','12M','-pix_fmt','yuv420p'); Fallback = @('-c:v','libx264','-crf','18','-pix_fmt','yuv420p');
        Color = @('-colorspace','bt709','-color_primaries','bt709','-color_trc','bt709','-color_range','tv') },
    @{ Name = 'hap'; Encode = @('-c:v','hap','-format','hap','-compressor',$HapCompressor,'-chunks',"$HapChunks"); Color = @() },
    @{ Name = 'hap-q'; Encode = @('-c:v','hap','-format','hap_q','-compressor',$HapCompressor,'-chunks',"$HapChunks"); Color = @() }
)
foreach ($fixture in $fixtures) {
    $fixture.Path = Join-Path $run ($fixture.Name + '.mov')
    Write-Host "Generazione $($fixture.Name)"
    $source = @('-hide_banner','-loglevel','error','-nostdin','-y',
        '-f','lavfi','-i',"${generator}=size=${Width}x${Height}:rate=$Fps",
        '-f','lavfi','-i','sine=frequency=440:sample_rate=48000',
        '-t',"$Seconds",'-c:a','pcm_s16le','-ac','2')
    $result = Invoke-VideoTool ($source + $fixture.Encode + $fixture.Color + $fixture.Path)
    if ($result.ExitCode -ne 0 -and $fixture.Fallback) {
        $result = Invoke-VideoTool ($source + $fixture.Fallback + $fixture.Color + $fixture.Path)
    }
    if ($result.ExitCode -ne 0) { throw $result.Stderr }
}

# Mirrors VideoTools.Filter at speed 1 and default placement.
$tail = "scale=w='max(1,round(iw*sar))':h=ih,setsar=1,setpts=PTS/1,fps=$Fps," +
    "scale=w='max(1,round(iw*min($Width/iw,$Height/ih)))':h='max(1,round(ih*min($Width/iw,$Height/ih)))'," +
    "setsar=1,format=bgra[clip];color=c=black@0:s=${Width}x${Height}:r=$Fps,format=bgra[bg];" +
    "[bg][clip]overlay=x='(W-w)/2':y='(H-h)/2':shortest=1:format=auto,format=bgra[out]"
$hardware = @('-hwaccel','d3d11va','-hwaccel_output_format','d3d11')
$vulkan = @('-init_hw_device','vulkan=bench','-hwaccel','vulkan','-hwaccel_device','bench','-hwaccel_output_format','vulkan')
$cases = @(
    @{ Name = 'h264-cpu'; Fixture = 0; InputArgs = @(); Prefix = '' },
    @{ Name = 'hap-cpu'; Fixture = 1; InputArgs = @(); Prefix = '' },
    @{ Name = 'hap-q-cpu'; Fixture = 2; InputArgs = @(); Prefix = '' },
    @{ Name = 'h264-d3d11-decode'; Fixture = 0; InputArgs = $hardware; Prefix = 'hwdownload,format=nv12,' },
    @{ Name = 'h264-vulkan-decode-color'; Fixture = 0; InputArgs = $vulkan; Graph = '[0:v:0]libplacebo=format=bgra:range=pc:upscaler=bilinear:downscaler=bilinear:dithering=none,hwdownload,format=bgra[out]' },
    @{ Name = 'hap-d3d11-bc1'; Fixture = 1; HapGpu = $true },
    @{ Name = 'hap-q-d3d11-bc3'; Fixture = 2; HapGpu = $true }
)
$frameBytes = $Width * $Height * 4
$expectedFrames = $Seconds * $Fps
$outputArgs = if ($DiscardFrames) { @('-progress','pipe:2','-y','NUL') } else { @('pipe:1') }
$rows = foreach ($attempt in 1..$Repeats) {
    foreach ($case in $cases) {
        $file = Get-Item -LiteralPath $fixtures[$case.Fixture].Path
        $executable = $Ffmpeg
        if ($case.HapGpu) {
            $executable = $hapBench
            $argsForCase = @($Ffmpeg,$file.FullName,"$Width","$Height",$fixtures[$case.Fixture].Name)
            if ($DiscardFrames) { $argsForCase += 'discard' }
        } else {
            $graph = if ($case.Graph) { $case.Graph } else { '[0:v:0]' + $case.Prefix + $tail }
            $argsForCase = @('-hide_banner','-loglevel','info','-nostats','-nostdin','-xerror','-benchmark','-threads','2') +
                $case.InputArgs + @('-i',$file.FullName,'-an','-sn','-dn','-filter_complex_threads','1',
                '-filter_complex',$graph,'-map','[out]',
                '-pix_fmt','bgra','-c:v','rawvideo','-threads','1','-f','rawvideo') + $outputArgs
        }
        Write-Host "$attempt/$Repeats $($case.Name)"
        $firstFrame = if ($attempt -eq 1 -and !$DiscardFrames) { Join-Path $run "$($case.Name)-first.bgra" }
        $readSize = if ($DiscardFrames) { 0 } else { $frameBytes }
        $sample = Invoke-VideoTool $argsForCase $readSize $executable $firstFrame
        $sample.Stderr | Set-Content (Join-Path $run "$($case.Name)-$attempt.log")
        $frames = $sample.Bytes / $frameBytes
        if ($DiscardFrames) {
            $reported = [regex]::Matches($sample.Stderr, '(?m)^frames?=(\d+)\s*$')
            if ($reported.Count) { $frames = [int]$reported[$reported.Count - 1].Groups[1].Value }
        }
        $ok = $sample.ExitCode -eq 0 -and $frames -eq $expectedFrames -and
            $sample.Stderr -notmatch 'VK_ERROR_|Failed dispatching|Failed to render'
        $cpu = $null
        if ($sample.Stderr -match 'utime=([0-9.]+)s stime=([0-9.]+)s') {
            $cpu = [double]$Matches[1] + [double]$Matches[2]
        }
        [pscustomobject]@{
            Case = $case.Name
            Run = $attempt
            Status = if ($ok) { 'OK' } else { 'Failed' }
            ExitCode = $sample.ExitCode
            FileMiB = [Math]::Round($file.Length / 1MB, 2)
            Frames = [Math]::Round($frames, 2)
            WallSeconds = [Math]::Round($sample.WallSeconds, 3)
            FirstFrameMs = $(if ($null -ne $sample.FirstFrameMs) { [Math]::Round($sample.FirstFrameMs, 1) })
            ThroughputFps = $(if ($ok) { [Math]::Round($expectedFrames / $sample.WallSeconds, 1) })
            FfmpegCpuSeconds = $cpu
        }
    }
}
$comparisons = foreach ($pair in @(
    @('h264-cpu','h264-vulkan-decode-color'),
    @('hap-cpu','hap-d3d11-bc1'),
    @('hap-q-cpu','hap-q-d3d11-bc3')
)) {
    $reference = Join-Path $run "$($pair[0])-first.bgra"
    $candidate = Join-Path $run "$($pair[1])-first.bgra"
    if (!(Test-Path -LiteralPath $reference) -or !(Test-Path -LiteralPath $candidate)) { continue }
    $raw = @('-f','rawvideo','-pixel_format','bgra','-video_size',"${Width}x${Height}")
    $comparison = Invoke-VideoTool (@('-hide_banner','-nostdin') + $raw + @('-i',$reference) +
        $raw + @('-i',$candidate,'-lavfi','psnr','-f','null','-'))
    if ($comparison.ExitCode -eq 0 -and $comparison.Stderr -match 'PSNR .* average:([\d.]+|inf)') {
        [pscustomobject]@{ Reference = $pair[0]; Candidate = $pair[1]; FirstFramePsnrDb = $Matches[1] }
    }
}
if ($comparisons) { $comparisons | Export-Csv -NoTypeInformation -Path (Join-Path $run 'first-frame-psnr.csv') }
$rows | Export-Csv -NoTypeInformation -Path (Join-Path $run 'results.csv')
$rows | Format-Table -AutoSize
Write-Host "Risultati e clip: $run"
if ($rows | Where-Object { $_.Case -like '*-cpu' -and $_.Status -ne 'OK' }) {
    throw 'Almeno una prova CPU ha fallito. Controllare i log nella directory del run.'
}
