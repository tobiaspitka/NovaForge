param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if($Dotnet -eq 'dotnet' -and !(Get-Command dotnet -ErrorAction SilentlyContinue)) {$Dotnet=Join-Path $root '.tools\dotnet\dotnet.exe'}
& $Dotnet build (Join-Path $root 'src\NovaForge\NovaForge.csproj') -c Release
if($LASTEXITCODE -ne 0){throw 'Build fehlgeschlagen.'}
& $Dotnet run --project (Join-Path $root 'tests\NovaForge.Tests\NovaForge.Tests.csproj') -c Release
if($LASTEXITCODE -ne 0){throw 'Tests fehlgeschlagen.'}
& (Join-Path $env:windir 'System32\WindowsPowerShell\v1.0\powershell.exe') -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests\Runner.Integration.ps1')
if($LASTEXITCODE -ne 0){throw 'PowerShell-Integrationstests fehlgeschlagen.'}
& $Dotnet run --project (Join-Path $root 'tests\NovaForge.UiTests\NovaForge.UiTests.csproj') -c Release -- (Join-Path $root 'artifacts\ui')
if($LASTEXITCODE -ne 0){throw 'WPF-Tests fehlgeschlagen.'}
& (Join-Path $PSScriptRoot 'Write-Validation.ps1')
