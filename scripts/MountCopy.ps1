param([Parameter(Mandatory=$true)][string]$Iso,[Parameter(Mandatory=$true)][string]$Destination,[string]$ProgressFile,[string]$CancelFile,[string]$ResultFile)
$ErrorActionPreference='Stop'
$owned=$false
try {
 $image=Get-DiskImage -ImagePath $Iso -ErrorAction SilentlyContinue
 if (!$image -or !$image.Attached) { $image=Mount-DiskImage -ImagePath $Iso -StorageType ISO -PassThru; $owned=$true }
 $volume=$image | Get-Volume
 if (!$volume.DriveLetter) { throw 'Das ISO-Laufwerk besitzt keinen Laufwerksbuchstaben.' }
 $root=$volume.DriveLetter+':\'
 if (!(Test-Path (Join-Path $root 'sources\boot.wim')) -or !(Test-Path (Join-Path $root 'setup.exe'))) { throw 'Kein vollständiges Windows-Setup-Medium.' }
 if (!(Test-Path (Join-Path $root 'sources\install.wim')) -and !(Test-Path (Join-Path $root 'sources\install.esd')) -and !(Test-Path (Join-Path $root 'sources\install.swm'))) { throw 'install.wim, install.esd oder install.swm fehlt.' }
 if (!(Test-Path (Join-Path $root 'efi\boot\bootx64.efi'))) { throw 'Nur Windows-x64-Medien mit bootx64.efi werden unterstützt.' }
 $bootWim=Join-Path $root 'sources\boot.wim'
 $dismLog=Join-Path ([IO.Path]::GetDirectoryName($ResultFile)) 'media-dism.log'
 $info=& (Join-Path $env:windir 'System32\dism.exe') /English /Get-WimInfo ("/WimFile:"+$bootWim) /Index:1 ("/LogPath:"+$dismLog) 2>&1
 if($LASTEXITCODE -ne 0){throw ('DISM konnte boot.wim nicht lesen: '+($info -join [Environment]::NewLine))}
 if(($info -join "`n") -notmatch 'Architecture\s*:\s*x64' -or ($info -join "`n") -notmatch 'Version\s*:\s*10\.') {throw 'Das Boot-Abbild ist kein Windows-10/11-x64-Abbild.'}
 $files=@(Get-ChildItem -LiteralPath $root -File -Recurse -Force)
 $total=($files | Measure-Object Length -Sum).Sum; $copied=0L
 $drive=[IO.DriveInfo]::new([IO.Path]::GetPathRoot($Destination))
 if ($drive.AvailableFreeSpace -lt $total+536870912) { throw 'Nicht genügend Speicher für den ISO-Inhalt.' }
 foreach($f in $files) {
  if(Test-Path -LiteralPath $CancelFile) { throw 'Abgebrochen.' }
  $target=Join-Path $Destination $f.FullName.Substring($root.Length)
  [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
  $inputStream=[IO.File]::OpenRead($f.FullName); $outputStream=[IO.File]::Create($target)
  try { $buffer=New-Object byte[] 1048576; while(($n=$inputStream.Read($buffer,0,$buffer.Length)) -gt 0) {
   if(Test-Path -LiteralPath $CancelFile) { throw 'Abgebrochen.' }
   $outputStream.Write($buffer,0,$n);$copied+=$n
   [IO.File]::WriteAllText($ProgressFile,"$copied/$total")
  }} finally { $inputStream.Dispose();$outputStream.Dispose() }
 }
 [IO.File]::WriteAllText($ResultFile,'OK')
} catch { [IO.File]::WriteAllText($ResultFile,$_.Exception.ToString()); exit 1 }
finally { if($owned) { try{Dismount-DiskImage -ImagePath $Iso -ErrorAction Stop}catch{[IO.File]::WriteAllText($ResultFile,'ISO konnte nicht freigegeben werden: '+$_.Exception.Message);exit 1} } }
