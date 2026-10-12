param([Parameter(Mandatory=$true)][string]$Iso,[Parameter(Mandatory=$true)][string]$Reference,[string]$ProgressFile,[string]$CancelFile,[string]$ResultFile)
$ErrorActionPreference='Stop';$owned=$false
try {
 $image=Get-DiskImage -ImagePath $Iso -ErrorAction SilentlyContinue
 if(!$image -or !$image.Attached){$image=Mount-DiskImage -ImagePath $Iso -StorageType ISO -PassThru;$owned=$true}
 $volume=$image | Get-Volume
 if(!$volume.DriveLetter){throw 'Ausgabe konnte nicht als ISO-Laufwerk gelesen werden.'}
 $root=$volume.DriveLetter+':\'
 $files=@(Get-ChildItem -LiteralPath $Reference -File -Recurse -Force);$count=0
 foreach($file in $files){
  if(Test-Path -LiteralPath $CancelFile){throw 'Abgebrochen.'}
  $relative=$file.FullName.Substring($Reference.TrimEnd('\').Length+1);$target=Join-Path $root $relative
  if(!(Test-Path -LiteralPath $target)){throw ('Datei fehlt in Ausgabe: '+$relative)}
  if((Get-Item -LiteralPath $target).Length -ne $file.Length){throw ('Dateigröße stimmt nicht: '+$relative)}
  if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash){throw ('Dateiprüfsumme stimmt nicht: '+$relative)}
  $count++;[IO.File]::WriteAllText($ProgressFile,"$count/$($files.Count)")
 }
 [xml]$answer=Get-Content -LiteralPath (Join-Path $root 'autounattend.xml') -Raw -Encoding UTF8
 if($answer.unattend.settings.pass -ne 'specialize'){throw 'Antwortdatei besitzt keine specialize-Phase.'}
 # El Torito boot record and boot catalog signature; booting each firmware still needs a VM.
 $stream=[IO.File]::OpenRead($Iso)
 try{
  $sector=New-Object byte[] 2048;$catalog=0L
  for($i=16;$i -lt 128;$i++){
   $stream.Position=$i*2048L;if($stream.Read($sector,0,2048) -ne 2048){break}
   if($sector[0] -eq 0 -and [Text.Encoding]::ASCII.GetString($sector,1,5) -eq 'CD001' -and [Text.Encoding]::ASCII.GetString($sector,7,23) -eq 'EL TORITO SPECIFICATION'){$catalog=[BitConverter]::ToUInt32($sector,71);break}
  }
  if($catalog -eq 0){throw 'El-Torito-Bootkatalog fehlt.'}
  $stream.Position=$catalog*2048L
  if($stream.Read($sector,0,2048) -ne 2048 -or $sector[0] -ne 1 -or $sector[30] -ne 0x55 -or $sector[31] -ne 0xAA -or $sector[32] -ne 0x88){throw 'Bootkatalog ungültig oder nicht bootfähig markiert.'}
  $sum=0;for($i=0;$i -lt 32;$i+=2){$sum+= [BitConverter]::ToUInt16($sector,$i)}
  if(($sum -band 0xffff) -ne 0){throw 'Bootkatalog-Prüfsumme ungültig.'}
 }finally{$stream.Dispose()}
 [IO.File]::WriteAllText($ResultFile,'OK')
}catch{[IO.File]::WriteAllText($ResultFile,$_.Exception.ToString());exit 1}
finally{if($owned){try{Dismount-DiskImage -ImagePath $Iso -ErrorAction Stop}catch{[IO.File]::WriteAllText($ResultFile,'ISO konnte nicht freigegeben werden: '+$_.Exception.Message);exit 1}}}
