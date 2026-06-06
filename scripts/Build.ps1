param(
  [switch]$Package
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$root = Split-Path -Parent $scriptDir
$dist = Join-Path $root "dist"
$exe = Join-Path $dist "AgentStatusLight.exe"
$sourceRoot = Join-Path $root "src"
$icon = Join-Path $root "assets\icons\AgentStatusLight.ico"

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
  $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
  throw "Could not find csc.exe from .NET Framework."
}
if (-not (Test-Path $icon)) {
  throw "Could not find application icon: $icon"
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null
$sources = Get-ChildItem -Path $sourceRoot -Filter "*.cs" -Recurse |
  Sort-Object FullName |
  ForEach-Object { $_.FullName }

$argsList = @(
  "/nologo",
  "/target:winexe",
  "/out:$exe",
  "/win32icon:$icon",
  "/reference:System.Windows.Forms.dll",
  "/reference:System.Drawing.dll",
  "/reference:System.Web.Extensions.dll"
)
$argsList += $sources

& $csc @argsList

if ($LASTEXITCODE -ne 0) {
  throw "AgentStatusLight.exe build failed."
}

Write-Host "Built dist\AgentStatusLight.exe"

if ($Package) {
  $zip = Join-Path $dist "AgentStatusLight.zip"
  if (Test-Path $zip) {
    Remove-Item -LiteralPath $zip -Force
  }
  Compress-Archive -LiteralPath $exe -DestinationPath $zip -Force
  Write-Host "Created dist\AgentStatusLight.zip"
}
