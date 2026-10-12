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
& (Join-Path $env:windir 'System32\WindowsPowerShell\v1.0\powershell.exe') -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests\Versioning.Integration.ps1')
if($LASTEXITCODE -ne 0){throw 'Versionierungstests fehlgeschlagen.'}
& $Dotnet run --project (Join-Path $root 'tests\NovaForge.UiTests\NovaForge.UiTests.csproj') -c Release -- (Join-Path $root 'artifacts\ui')
if($LASTEXITCODE -ne 0){throw 'WPF-Tests fehlgeschlagen.'}
$oldRuntime=$env:DOTNET_ROOT
try {
 $dotnetExecutable=if(Test-Path -LiteralPath $Dotnet){[IO.Path]::GetFullPath($Dotnet)}else{(Get-Command $Dotnet).Source};$env:DOTNET_ROOT=Split-Path $dotnetExecutable
 & $Dotnet run --project (Join-Path $root 'tests\NovaForge.AutomationTests\NovaForge.AutomationTests.csproj') -c Release -- (Join-Path $root 'src\NovaForge\bin\Release\net10.0-windows\NovaForge.Beta.exe')
 if($LASTEXITCODE -ne 0){throw 'Recorder-/Wiedergabetests fehlgeschlagen.'}
}finally{$env:DOTNET_ROOT=$oldRuntime}
& (Join-Path $PSScriptRoot 'Write-Validation.ps1')
