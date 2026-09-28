<#
  Builds Grade Tracker and its MSI installer.

  Usage (PowerShell, from the repository root):
      .\build.ps1
      .\build.ps1 -Configuration Debug

  Steps:
    1. dotnet publish  -> .\publish\  (GradeTracker.exe + all DLLs)
    2. dotnet build    -> WiX v7 compiles .\installer\... into GradeTrackerSetup.msi
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root      = $PSScriptRoot
$publish   = Join-Path $root "publish"
$appProj   = Join-Path $root "src\GradeTracker.App\GradeTracker.App.csproj"
$wixProj   = Join-Path $root "installer\GradeTracker.Installer\GradeTracker.Installer.wixproj"

Write-Host "`n=== 1/3 Publishing application ($Configuration, win-x64) ===" -ForegroundColor Cyan
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

dotnet publish $appProj -c $Configuration -r win-x64 --self-contained false `
    -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)" }

Write-Host "`nFiles that will be packaged:" -ForegroundColor Cyan
Get-ChildItem $publish | Format-Table Name, Length -AutoSize

Write-Host "=== 2/3 Building MSI with WiX Toolset v7 ===" -ForegroundColor Cyan
dotnet build $wixProj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "WiX build failed (exit code $LASTEXITCODE)" }

Write-Host "`n=== 3/3 Result ===" -ForegroundColor Cyan
$msi = Get-ChildItem (Join-Path $root "installer\GradeTracker.Installer\bin") -Recurse -Filter *.msi |
       Sort-Object LastWriteTime -Descending | Select-Object -First 1
$hash = Get-FileHash $msi.FullName -Algorithm SHA256
Write-Host "MSI    : $($msi.FullName)"
Write-Host "Size   : $([math]::Round($msi.Length / 1KB, 1)) KB"
Write-Host "SHA256 : $($hash.Hash)"
