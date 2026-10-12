param([string]$StorageDirectory=(Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'NovaForge\Versions'))
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot;. (Join-Path $PSScriptRoot 'VersionSources.ps1')
$projectFile=Join-Path $root 'src\NovaForge\NovaForge.csproj';[xml]$project=Get-Content -LiteralPath $projectFile -Raw
$version=[string]$project.Project.PropertyGroup.Version
$directory=Join-Path $StorageDirectory $version
if(!(Test-Path -LiteralPath $directory)){return}
$metadata=Get-ChildItem -LiteralPath $directory -Filter version.json -Recurse -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if(!$metadata){return}
$previous=Get-Content -LiteralPath $metadata.FullName -Raw | ConvertFrom-Json
if(!$previous.Files){return}
$files=@(Get-NovaForgeVersionSources $root);$changed=$files.Count -ne @($previous.Files).Count
foreach($file in $files){$relative=$file.Substring($root.Length+1).Replace('\','/');$record=@($previous.Files | Where-Object Path -eq $relative);if($record.Count -ne 1 -or $record[0].Sha256 -ne (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash){$changed=$true;break}}
if($changed){$current=[Version]$version;$next='{0}.{1}.{2}' -f $current.Major,$current.Minor,($current.Build+1);$project.Project.PropertyGroup.Version=$next;$project.Save($projectFile);Write-Output ('Quellstand geändert: neue Version '+$next+'. Vorheriger geprüfter Stand bleibt erhalten.')}
