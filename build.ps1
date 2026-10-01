$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "src\BannerlordModDoctor.cs"
$dist = Join-Path $root "dist"
$out = Join-Path $dist "BannerlordModDoctor.exe"

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "Could not find .NET Framework csc.exe"
}

& $csc `
    /nologo `
    /target:winexe `
    /out:$out `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Xml.dll `
    /reference:System.Xml.Linq.dll `
    $src

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built $out"
