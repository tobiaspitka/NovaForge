param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
& (Join-Path $PSScriptRoot 'Ensure-Version.ps1')
if($Dotnet -eq 'dotnet' -and !(Get-Command dotnet -ErrorAction SilentlyContinue)) {$Dotnet=Join-Path $root '.tools\dotnet\dotnet.exe'}
& (Join-Path $PSScriptRoot 'Build.ps1') -Dotnet $Dotnet
& $Dotnet publish (Join-Path $root 'src\NovaForge\NovaForge.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o (Join-Path $root 'dist\win-x64')
if($LASTEXITCODE -ne 0){throw 'Publish fehlgeschlagen.'}
$licenses=Join-Path $root 'dist\win-x64\licenses';New-Item -ItemType Directory -Force $licenses | Out-Null
$packageLocation=(& $Dotnet nuget locals global-packages --list) -replace '^[^:]+:\s*',''
$config=Get-Content -LiteralPath (Join-Path $root 'src\NovaForge\bin\Release\net10.0-windows\win-x64\NovaForge.runtimeconfig.json') -Raw | ConvertFrom-Json
foreach($framework in $config.runtimeOptions.includedFrameworks){
 $packageName=($framework.name+'.runtime.win-x64').ToLowerInvariant()
 $package=Join-Path $packageLocation ($packageName+'\'+$framework.version)
 Get-ChildItem -LiteralPath $package -File | Where-Object Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT|ThirdPartyNotices\.txt)$' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $licenses ($framework.name+'-'+$_.Name))}
}
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $licenses 'NovaForge-LICENSE.txt')
& (Join-Path $root 'tests\Portable.Smoke.ps1')
& (Join-Path $PSScriptRoot 'Store-Version.ps1')
Get-FileHash (Join-Path $root 'dist\win-x64\NovaForge.exe') -Algorithm SHA256 | Format-List
