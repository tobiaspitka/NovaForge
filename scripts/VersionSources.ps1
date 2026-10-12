function Get-NovaForgeVersionSources([string]$Root) {
 $files=@()
 foreach($folder in @('src','scripts','tests','examples','.github')){
  $path=Join-Path $Root $folder
  if(Test-Path -LiteralPath $path){$files+=Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Name -ne 'runner-results.txt' } | Select-Object -ExpandProperty FullName}
 }
 foreach($name in @('README.md','ARCHITEKTUR.md','TESTPROTOKOLL.md','CHANGELOG.md','TODO.md','LICENSE','global.json','Directory.Build.props','.gitignore','.gitattributes')){if(Test-Path -LiteralPath (Join-Path $Root $name)){$files+=Join-Path $Root $name}}
 return @($files | Sort-Object -Unique)
}
function Get-NovaForgeSourceDigest([string]$Root) {
 $lines=@(Get-NovaForgeVersionSources $Root | ForEach-Object {$_.Substring($Root.Length+1).Replace('\','/')+':'+(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash})
 $hasher=[Security.Cryptography.SHA256]::Create()
 try{return [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n")))).Replace('-','')}finally{$hasher.Dispose()}
}
function Get-NovaForgeBuildInfo([string]$Root){
 [xml]$project=Get-Content -LiteralPath (Join-Path $Root 'src\NovaForge\NovaForge.csproj') -Raw
 $version=[string]$project.Project.PropertyGroup.Version;$name=[string]$project.Project.PropertyGroup.AssemblyName;if(!$name){$name='NovaForge'}
 $beta=$version.Contains('-');$directory=Join-Path $Root $(if($beta){'dist\beta\win-x64'}else{'dist\win-x64'})
 return [PSCustomObject]@{Version=$version;AssemblyName=$name;Beta=$beta;OutputDirectory=$directory;ExePath=(Join-Path $directory ($name+'.exe'));Title=$(if($beta){'NovaForge Beta · Installationsstudio'}else{'NovaForge · Windows nach deinem Plan'})}
}
