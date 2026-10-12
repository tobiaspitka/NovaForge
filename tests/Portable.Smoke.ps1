param([string]$Exe=(Join-Path (Split-Path $PSScriptRoot) 'dist\win-x64\NovaForge.exe'),[string]$Report=(Join-Path (Split-Path $PSScriptRoot) 'artifacts\portable-start.txt'))
$ErrorActionPreference='Stop'
$testDirectory=Join-Path ([IO.Path]::GetTempPath()) ('NovaForge-Portable-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $testDirectory | Out-Null
$oldRoot=$env:DOTNET_ROOT;$oldRootX64=$env:DOTNET_ROOT_X64;$process=$null
try{
 Copy-Item -LiteralPath $Exe -Destination (Join-Path $testDirectory 'NovaForge.exe')
 $env:DOTNET_ROOT=Join-Path $testDirectory 'nonexistent-runtime';$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT
 $process=Start-Process -FilePath (Join-Path $testDirectory 'NovaForge.exe') -WorkingDirectory $testDirectory -PassThru
 $deadline=(Get-Date).AddSeconds(25)
 do{Start-Sleep -Milliseconds 500;$process.Refresh()}while(!$process.HasExited -and $process.MainWindowTitle -ne 'NovaForge · Windows nach deinem Plan' -and (Get-Date) -lt $deadline)
 if($process.HasExited -or $process.MainWindowTitle -ne 'NovaForge · Windows nach deinem Plan'){throw ('EXE-Start fehlgeschlagen: '+$process.MainWindowTitle)}
 $process.CloseMainWindow() | Out-Null
 if(!$process.WaitForExit(5000) -or $process.ExitCode -ne 0){throw 'Anwendung wurde nicht normal beendet.'}
 $message='PASS: standalone WPF EXE opened and closed with exitcode 0; no adjacent DLLs; DOTNET_ROOT and DOTNET_ROOT_X64 invalid. SHA256='+ (Get-FileHash -LiteralPath $Exe -Algorithm SHA256).Hash
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Report))) | Out-Null
 $message | Set-Content -LiteralPath $Report -Encoding UTF8;Write-Output $message
}finally{
 $env:DOTNET_ROOT=$oldRoot;$env:DOTNET_ROOT_X64=$oldRootX64
 if($process -and !$process.HasExited){Stop-Process -Id $process.Id}
 $resolved=[IO.Path]::GetFullPath($testDirectory);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath());if(!$resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsicherer Bereinigungspfad.'};Remove-Item -LiteralPath $resolved -Recurse -Force
}
