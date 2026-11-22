$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $root 'bin'
$vendor = Join-Path $root 'third-party\binaries'
$packages = @{
    'SharpDX' = '117126040200FAAA118F1312AD5A5B1E7D067C90ABF6E726F18B6D1635216FC3'
    'SharpDX.DXGI' = 'CB4A31BD0137EC8845B234621A690E5AB2B45830BDE0A3D27CAABC5BCED5FC97'
    'SharpDX.Direct3D11' = '21DEDCF8B1DDB50EA3FF81F61D45511AEC79D727D15BDA9AE45C05D5058EE219'
    'SharpDX.D3DCompiler' = 'CA86D6A315FDEA7C0CC7C25299B66FA4F8DC8BAED5B7C3ED918FEC9BBABFC726'
}
New-Item -ItemType Directory -Force $destination | Out-Null
foreach ($name in $packages.Keys) {
    $target = Join-Path $destination ($name + '.dll')
    if (Test-Path -LiteralPath $target) { continue }
    $vendored = Join-Path $vendor ($name + '.dll')
    if (Test-Path -LiteralPath $vendored) {
        Copy-Item -LiteralPath $vendored -Destination $target -Force
        continue
    }
    $package = Join-Path $destination ($name + '.4.2.0.zip')
    $folder = Join-Path $destination ($name + '-package')
    try {
        $url = 'https://api.nuget.org/v3-flatcontainer/' + $name.ToLowerInvariant() + '/4.2.0/' + $name.ToLowerInvariant() + '.4.2.0.nupkg'
        Invoke-WebRequest -Uri $url -OutFile $package
        if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $packages[$name]) { throw "Hash SharpDX non valido: $name" }
        Expand-Archive -LiteralPath $package -DestinationPath $folder -Force
        Copy-Item -LiteralPath (Join-Path $folder ('lib\net45\' + $name + '.dll')) -Destination $target
    } finally {
        Remove-Item -LiteralPath $package -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
    }
}
