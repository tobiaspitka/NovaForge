param([string]$StorageDirectory=(Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'NovaForge\Versions'))
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot;. (Join-Path $PSScriptRoot 'VersionSources.ps1')
[xml]$project=Get-Content -LiteralPath (Join-Path $root 'src\NovaForge\NovaForge.csproj') -Raw;$version=[string]$project.Project.PropertyGroup.Version
$exe=Join-Path $root 'dist\win-x64\NovaForge.exe';$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
$proof=Join-Path $root 'artifacts\portable-start.txt'
if(!(Test-Path -LiteralPath $proof) -or (Get-Content -LiteralPath $proof -Raw) -notmatch [Regex]::Escape($hash)){throw 'Version wird nicht gesichert: erfolgreicher Startnachweis für diese EXE fehlt.'}
$validationFile=Join-Path $root 'artifacts\validation.json'
if(!(Test-Path -LiteralPath $validationFile)){throw 'Build-/Testnachweis fehlt.'}
$validation=Get-Content -LiteralPath $validationFile -Raw | ConvertFrom-Json
if($validation.Version -ne $version -or !$validation.CoreAndProcessTestsPassed -or $validation.SourceDigest -ne (Get-NovaForgeSourceDigest $root)){throw 'Quellcode wurde seit den erfolgreichen Tests verändert. Erst neu bauen und testen.'}
$versionRoot=Join-Path $StorageDirectory $version
if(Test-Path -LiteralPath $versionRoot){foreach($recordFile in Get-ChildItem -LiteralPath $versionRoot -Filter version.json -Recurse -File){$record=Get-Content -LiteralPath $recordFile.FullName -Raw | ConvertFrom-Json;if($record.ExeSha256 -ne $hash){throw 'Eine andere EXE mit dieser Versionsnummer ist bereits gesichert. Versionsnummer erhöhen.'}}}
$stamp=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-ffff');$pending=Join-Path $StorageDirectory ('.pending-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Force $pending | Out-Null
try{
 Copy-Item -LiteralPath $exe -Destination (Join-Path $pending ('NovaForge-'+$version+'.exe'))
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $sourceZip=Join-Path $pending ('NovaForge-'+$version+'-Quellcode.zip');$zip=[IO.Compression.ZipFile]::Open($sourceZip,[IO.Compression.ZipArchiveMode]::Create);$records=@()
 try{foreach($file in Get-NovaForgeVersionSources $root){$relative=$file.Substring($root.Length+1).Replace('\','/');$records+=@{Path=$relative;Sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash};[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file,$relative,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}}finally{$zip.Dispose()}
 foreach($proofFile in @('final-build.txt','portable-start.txt')){$file=Join-Path $root ('artifacts\'+$proofFile);if(Test-Path -LiteralPath $file){Copy-Item -LiteralPath $file -Destination $pending}}
 $metadata=@{Version=$version;SavedAt=[DateTimeOffset]::Now.ToString('o');ExeSha256=$hash;SourceSha256=(Get-FileHash -LiteralPath $sourceZip).Hash;Validation='Build, Kern-/Prozessprüfungen, UI-Layout und EXE-Start bestanden; ISO-/VM-Prüfung getrennt';Files=$records}
 $metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $pending 'version.json') -Encoding UTF8
 New-Item -ItemType Directory -Force $versionRoot | Out-Null
 Move-Item -LiteralPath $pending -Destination (Join-Path $versionRoot $stamp)
 Write-Output ('Geprüfte Version '+$version+' lokal gesichert: '+(Join-Path $versionRoot $stamp))
}finally{if(Test-Path -LiteralPath $pending){$resolved=[IO.Path]::GetFullPath($pending);$allowed=[IO.Path]::GetFullPath($StorageDirectory).TrimEnd('\')+'\';if(!$resolved.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Ungültiger Bereinigungspfad'};Remove-Item -LiteralPath $resolved -Recurse -Force}}
