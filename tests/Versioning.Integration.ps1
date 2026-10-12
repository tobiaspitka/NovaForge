$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot
$temporary=Join-Path ([IO.Path]::GetTempPath()) ('NovaForge-VersionTest-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force (Join-Path $temporary 'scripts'),(Join-Path $temporary 'src\NovaForge'),(Join-Path $temporary 'versions\9.0.0\checkpoint'),(Join-Path $temporary 'dist\win-x64'),(Join-Path $temporary 'artifacts') | Out-Null
try {
 foreach($name in @('Ensure-Version.ps1','Store-Version.ps1','VersionSources.ps1')){Copy-Item -LiteralPath (Join-Path $root ('scripts\'+$name)) -Destination (Join-Path $temporary ('scripts\'+$name))}
 $project=Join-Path $temporary 'src\NovaForge\NovaForge.csproj';[IO.File]::WriteAllText($project,'<Project><PropertyGroup><Version>9.0.0</Version></PropertyGroup></Project>')
 $source=Join-Path $temporary 'src\NovaForge\sample.cs';[IO.File]::WriteAllText($source,'original')
 . (Join-Path $temporary 'scripts\VersionSources.ps1')
 $files=@(Get-NovaForgeVersionSources $temporary | ForEach-Object {@{Path=$_.Substring($temporary.Length+1).Replace('\','/');Sha256=(Get-FileHash -LiteralPath $_).Hash}})
 $metadata=Join-Path $temporary 'versions\9.0.0\checkpoint\version.json';@{Version='9.0.0';ExeSha256='retained';Files=$files} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadata -Encoding UTF8
 $oldHash=(Get-FileHash -LiteralPath $metadata).Hash
 & (Join-Path $temporary 'scripts\Ensure-Version.ps1') -StorageDirectory (Join-Path $temporary 'versions')
 [xml]$p=Get-Content -LiteralPath $project;if($p.Project.PropertyGroup.Version -ne '9.0.0'){throw 'Unveränderter Stand erhielt neue Nummer'};Write-Output 'PASS Unchanged source retains version'
 [IO.File]::WriteAllText($source,'changed')
 & (Join-Path $temporary 'scripts\Ensure-Version.ps1') -StorageDirectory (Join-Path $temporary 'versions')
 [xml]$p=Get-Content -LiteralPath $project;if($p.Project.PropertyGroup.Version -ne '9.0.1'){throw 'Änderung erhielt keine Patchversion'};Write-Output 'PASS Changed source automatically increments patch version'
 if((Get-FileHash -LiteralPath $metadata).Hash -ne $oldHash){throw 'Vorheriger Versionsstand verändert'};Write-Output 'PASS Previous checkpoint remains byte-identical'
 $dummy=Join-Path $temporary 'dist\win-x64\NovaForge.exe';[IO.File]::WriteAllText($dummy,'test data, not executable')
 [IO.File]::WriteAllText((Join-Path $temporary 'artifacts\portable-start.txt'),'fixture hash: '+(Get-FileHash -LiteralPath $dummy).Hash)
 $rejected=$false
 try{& (Join-Path $temporary 'scripts\Store-Version.ps1') -StorageDirectory (Join-Path $temporary 'versions')}catch{$rejected=$true}
 if(!$rejected){throw 'Ungetestete Sicherung wurde angenommen'};Write-Output 'PASS Snapshot rejects missing build/test proof'
}finally{
 $resolved=[IO.Path]::GetFullPath($temporary);$allowed=[IO.Path]::GetFullPath([IO.Path]::GetTempPath());if(!$resolved.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Ungültiger Testpfad'};Remove-Item -LiteralPath $resolved -Recurse -Force
}
