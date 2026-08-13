# Publish Opening Times widget for MSIX / Partner Center packaging.
# Pause Dropbox first. Run in PowerShell from anywhere.

$ErrorActionPreference = "Stop"
$Root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
# script is in ...\Opening Times - Stores\scripts
$Root = Split-Path $PSScriptRoot -Parent
Set-Location $Root

Write-Host "Publishing to publish\win-x64 ..." -ForegroundColor Cyan
dotnet publish ".\OpeningTimesWidget\OpeningTimesWidget.csproj" `
  -c Release -r win-x64 --self-contained false `
  -o ".\publish\win-x64"

Write-Host "Secret scan..." -ForegroundColor Cyan
$hits = Get-ChildItem ".\publish\win-x64" -Recurse -File |
  Select-String -Pattern "xai-[A-Za-z0-9]" -SimpleMatch -ErrorAction SilentlyContinue
if ($hits) {
  Write-Warning "Possible secret in publish output — fix before Store upload"
  $hits | ForEach-Object { $_.Path }
} else {
  Write-Host "No xai- tokens found in publish folder." -ForegroundColor Green
}

Write-Host "Done: $Root\publish\win-x64" -ForegroundColor Green
Write-Host "Next: package with VS Packaging Project or MSIX Packaging Tool using Package\Package.appxmanifest"
