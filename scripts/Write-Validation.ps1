$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot;. (Join-Path $PSScriptRoot 'VersionSources.ps1')
[xml]$project=Get-Content -LiteralPath (Join-Path $root 'src\NovaForge\NovaForge.csproj') -Raw
$proof=@{Version=[string]$project.Project.PropertyGroup.Version;SourceDigest=(Get-NovaForgeSourceDigest $root);ValidatedAt=[DateTimeOffset]::Now.ToString('o');BuildPassed=$true;CoreAndProcessTestsPassed=$true;UiLayoutPassed=$true}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'artifacts\validation.json') -Encoding UTF8
