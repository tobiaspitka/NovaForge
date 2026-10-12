param([switch]$SkipPublish,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot
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
$portableZip=Join-Path $release 'NovaForge-1.0-win-x64.zip';Archive $portableZip $portable @((Get-ChildItem -LiteralPath $portable -Recurse -File).FullName)
$sourceFiles=@();foreach($folder in @('src','scripts','tests','examples','artifacts')){$sourceFiles+=@(Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File | Where-Object FullName -notmatch '[\\/](bin|obj)[\\/]' | Select-Object -ExpandProperty FullName)}
$sourceFiles+=@('README.md','ARCHITEKTUR.md','TESTPROTOKOLL.md','LICENSE','global.json','.gitignore' | ForEach-Object {Join-Path $root $_})
$sourceZip=Join-Path $release 'NovaForge-1.0-Quellcode.zip';Archive $sourceZip $root $sourceFiles
@($portableZip,$sourceZip,(Join-Path $portable 'NovaForge.exe')) | ForEach-Object {(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash+'  '+[IO.Path]::GetFileName($_)} | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding UTF8
Get-ChildItem -LiteralPath $release -File | Select-Object Name,Length
