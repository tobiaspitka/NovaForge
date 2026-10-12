param([switch]$SkipPublish,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot
[xml]$project=Get-Content -LiteralPath (Join-Path $root 'src\NovaForge\NovaForge.csproj') -Raw
if(!$SkipPublish){& (Join-Path $PSScriptRoot 'Publish.ps1') -Dotnet $Dotnet}
$release=Join-Path $root 'release';$portable=Join-Path $release 'NovaForge-portable';New-Item -ItemType Directory -Force $release,$portable | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'dist\win-x64\NovaForge.exe') -Destination (Join-Path $portable 'NovaForge.exe') -Force
$licenses=Join-Path $portable 'licenses';New-Item -ItemType Directory -Force $licenses | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'dist\win-x64\licenses') -File | Where-Object { $_.Name -like 'Microsoft.*' -or $_.Name -eq 'NovaForge-LICENSE.txt' } | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $licenses -Force}
foreach($name in @('README.md','ARCHITEKTUR.md','TESTPROTOKOLL.md','LICENSE')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $portable -Force}
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Archive([string]$path,[string]$base,[string[]]$files){
 if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path -Force}
 $zip=[IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
 try{foreach($file in $files){$entry=$file.Substring($base.TrimEnd('\').Length+1).Replace('\','/');[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file,$entry,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}}finally{$zip.Dispose()}
}
[xml]$project=Get-Content -LiteralPath (Join-Path $root 'src\NovaForge\NovaForge.csproj') -Raw;$version=[string]$project.Project.PropertyGroup.Version
$portableZip=Join-Path $release ('NovaForge-'+$version+'-win-x64.zip');Archive $portableZip $portable @((Get-ChildItem -LiteralPath $portable -Recurse -File).FullName)
. (Join-Path $PSScriptRoot 'VersionSources.ps1');$sourceFiles=Get-NovaForgeVersionSources $root
$sourceZip=Join-Path $release ('NovaForge-'+$version+'-Quellcode.zip');Archive $sourceZip $root $sourceFiles
$versionExe=Join-Path $release ('NovaForge-'+$version+'.exe');Copy-Item -LiteralPath (Join-Path $portable 'NovaForge.exe') -Destination $versionExe -Force
@($portableZip,$sourceZip,$versionExe) | ForEach-Object {(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash+'  '+[IO.Path]::GetFileName($_)} | Set-Content -LiteralPath (Join-Path $release ('SHA256SUMS-'+$version+'.txt')) -Encoding UTF8
Get-ChildItem -LiteralPath $release -File | Select-Object Name,Length
