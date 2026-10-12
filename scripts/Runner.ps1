param([ValidateSet('System','User')][string]$Context='System',[switch]$SetupPhase)
$ErrorActionPreference='Stop'
$root=Join-Path $env:windir 'NovaForge'
$base=if($Context -eq 'System') { Join-Path $root 'State' } else { Join-Path $env:LOCALAPPDATA 'NovaForge' }
New-Item -ItemType Directory -Force $base | Out-Null
$log=Join-Path $base 'installation.log'
function Log([string]$message) { Add-Content -LiteralPath $log -Value ((Get-Date -Format o)+' '+$message) -Encoding UTF8 }
function Read-State([string]$file) {
 $s=@{}; if(Test-Path -LiteralPath $file) { $o=Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json; foreach($property in $o.PSObject.Properties) { $s[$property.Name]=$property.Value } }; return $s
}
$stateFile=Join-Path $base 'state.json'
function Save-State { $tmp=$stateFile+'.tmp';[IO.File]::WriteAllText($tmp,($script:state | ConvertTo-Json -Depth 10)); if(Test-Path -LiteralPath $stateFile) {$backup=$stateFile+'.bak';[IO.File]::Replace($tmp,$stateFile,$backup);[IO.File]::Delete($backup)} else {[IO.File]::Move($tmp,$stateFile)} }
$mutexName=if($Context -eq 'System') {'Global\NovaForge-System'} else {'Local\NovaForge-User'}
$mutex=New-Object Threading.Mutex($false,$mutexName);$locked=$false
try {
 try {$locked=$mutex.WaitOne(0)} catch [Threading.AbandonedMutexException] {$locked=$true}
 if(!$locked) {return}
 if(!$SetupPhase -and $Context -eq 'System' -and (Get-ItemProperty 'HKLM:\SYSTEM\Setup' -Name SystemSetupInProgress).SystemSetupInProgress -ne 0) {return}
 $manifest=Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 $script:state=Read-State $stateFile
 $boot=(Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToUniversalTime().ToString('o')
 if($state.ContainsKey('_reboot') -and $state['_reboot'] -eq $boot) {Log 'Neustart erforderlich. Warteschlange wartet auf einen manuellen Windows-Neustart.';return}
 $state.Remove('_reboot'); Save-State
 $systemState=if($Context -eq 'User') { Read-State (Join-Path $root 'State\state.json') } else {$state}
 if($Context -eq 'User' -and $systemState.ContainsKey('_reboot') -and $systemState['_reboot'] -eq $boot) {Log 'Systemwarteschlange wartet auf Neustart. Benutzerinstallation wird danach fortgesetzt.';return}
 if($Context -eq 'User') {
  $waitStart=Get-Date;$announced=$false
  while($true){
   $systemState=Read-State (Join-Path $root 'State\state.json')
   if($systemState.ContainsKey('_reboot') -and $systemState['_reboot'] -eq $boot){Log 'Systeminstallation benötigt Neustart. Benutzerwarteschlange setzt bei nächster Anmeldung fort.';return}
   $pendingSystem=@($manifest.Programs | Where-Object {$_.Context -eq 'System' -and (!$systemState.ContainsKey($_.Id) -or $systemState[$_.Id].Status -in @('Running','Pending'))})
   if($pendingSystem.Count -eq 0){break}
   if(!$announced){Log 'Warte im Hintergrund auf Abschluss der Systemwarteschlange.';$announced=$true}
   if(((Get-Date)-$waitStart).TotalHours -ge 24){Log 'Systemwarteschlange nach 24 Stunden nicht abgeschlossen. Fortsetzung bei nächster Anmeldung.';return}
   Start-Sleep -Seconds 2
  }
 }
 foreach($app in @($manifest.Programs | Where-Object Context -eq $Context)) {
  if($state.ContainsKey($app.Id)) {
   if($state[$app.Id].Status -eq 'Running') {$state[$app.Id]=@{Status='Interrupted';Message='Prozess durch Neustart/Abbruch unterbrochen. Manuelle Prüfung erforderlich.'};Save-State}
   continue
  }
  $wait=$false;$blocked=$false
  foreach($dep in @($app.DependencyIds)) {
   $other=$manifest.Programs | Where-Object Id -eq $dep
   $depState=if($other.Context -eq 'System') {$systemState} else {$state}
   if(!$depState.ContainsKey($dep)) {$wait=$true} elseif($depState[$dep].Status -notin @('Success','RebootRequired')) {$blocked=$true}
  }
  if($wait) {Log ('Abhängigkeit noch nicht abgeschlossen: '+$app.Name);return}
  if($blocked) {$state[$app.Id]=@{Status='Blocked';Message='Abhängigkeit fehlgeschlagen.'};Save-State;continue}
  $installer=Join-Path $root ('Installers\'+$app.Id+[IO.Path]::GetExtension($app.Path))
  if($app.InstallerRelativePath){$packageRoot=[IO.Path]::GetFullPath((Join-Path $root ('Installers\'+$app.Id)));$installer=[IO.Path]::GetFullPath((Join-Path (Join-Path $root 'Installers') $app.InstallerRelativePath));if(!$installer.StartsWith($packageRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Ungültiger Installer-Paketpfad.'}}
  $process=$null
  try {
   if((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash -ne $app.Sha256) {throw 'SHA-256-Prüfsumme stimmt nicht überein.'}
   foreach($file in @($app.PackageFiles)){if(!$file){continue};$path=[IO.Path]::GetFullPath((Join-Path $packageRoot $file.Path));if(!$path.StartsWith($packageRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256){throw 'Installer-Begleitdatei fehlt oder wurde verändert.'}}
   $state[$app.Id]=@{Status='Running';Started=(Get-Date -Format o)};Save-State
   Log ('Start: '+$app.Name+' ('+$Context+')')
   $info=New-Object Diagnostics.ProcessStartInfo
   $info.UseShellExecute=$false; $info.WorkingDirectory=[IO.Path]::GetDirectoryName($installer)
   if($app.AutomationMode -eq 'Recorded') {
    if($Context -ne 'User'){throw 'Recorder-Installation benötigt einen angemeldeten Benutzer.'}
    $profile=Join-Path $root ('Profiles\'+$app.Id+'.json')
    if((Get-FileHash -LiteralPath $profile -Algorithm SHA256).Hash -ne $app.RecordedProfileSha256){throw 'Recorder-Profil-Prüfsumme stimmt nicht.'}
    $info.FileName=Join-Path $root 'NovaForge.Beta.exe'
    $replayLog=Join-Path $base ($app.Id+'.recorder.log')
    $info.Arguments='--replay --profile "'+$profile+'" --installer "'+$installer+'" --log "'+$replayLog+'"'
   } elseif($app.Type -eq 'MSI') {
    $info.FileName=Join-Path $env:windir 'System32\msiexec.exe'
    $msiLog=Join-Path $base ($app.Id+'.msi.log')
    $info.Arguments='/i "'+$installer+'" '+$app.Arguments+' /qn /norestart REBOOT=ReallySuppress /L*v "'+$msiLog+'"'
    if($Context -eq 'System') {$info.Arguments+=' ALLUSERS=1'} else {$info.Arguments+=' ALLUSERS=2 MSIINSTALLPERUSER=1'}
   } else { $info.FileName=$installer;$info.Arguments=$app.Arguments }
   $process=[Diagnostics.Process]::Start($info)
   if(!$process.WaitForExit([int]$app.TimeoutSeconds*1000)) {
    & (Join-Path ([Environment]::GetFolderPath('System')) 'taskkill.exe') /PID $process.Id /T /F | Out-Null
    $state[$app.Id]=@{Status='Timeout';Message='Zeitlimit überschritten; Prozessbaum beendet.'};Log ('Zeitüberschreitung: '+$app.Name)
   } else {
    $code=$process.ExitCode
    $status=if($code -eq 1641) {'UnexpectedReboot'} elseif($code -notin @($app.AllowedCodes)) {'Failed'} elseif($code -eq 3010) {'RebootRequired'} else {'Success'}
    $state[$app.Id]=@{Status=$status;ExitCode=$code;Finished=(Get-Date -Format o)}
    Log ($app.Name+': '+$status+' / Exitcode '+$code)
    if($status -eq 'RebootRequired' -and $app.RebootPolicy -eq 'PauseUntilReboot') { $state['_reboot']=$boot;Save-State;Log 'Bitte Windows nach Abschluss des Setups manuell neu starten.';return }
   }
  } catch {$state[$app.Id]=@{Status='Failed';Message=$_.Exception.Message};Log ($app.Name+': '+$_.Exception.Message)}
  finally {if($process) {$process.Dispose()};Save-State}
 }
 $apps=@($manifest.Programs | Where-Object Context -eq $Context)
 $success=@($apps | Where-Object { !$state.ContainsKey($_.Id) -or $state[$_.Id].Status -notin @('Success','RebootRequired') }).Count -eq 0
 if($success) {
  Log 'Warteschlange erfolgreich abgeschlossen. Protokolle und Status bleiben erhalten.'
  if($Context -eq 'User') {[IO.File]::WriteAllText((Join-Path $env:ProgramData ('NovaForge\UserCompletion\'+$manifest.BuildId+'.done')),'completed')}
  if($Context -eq 'System' -and !$SetupPhase) {
   $hasUser=@($manifest.Programs | Where-Object Context -eq 'User').Count -gt 0
   if(!$hasUser -or (Test-Path -LiteralPath (Join-Path $env:ProgramData ('NovaForge\UserCompletion\'+$manifest.BuildId+'.done')))) {
    Unregister-ScheduledTask -TaskName 'NovaForge-User' -Confirm:$false -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName 'NovaForge-System' -Confirm:$false -ErrorAction SilentlyContinue
   }
  }
 } else {
  Log 'Warteschlange beendet mit Fehlern / blockierten Installationen. Details in state.json und den einzelnen Protokolleinträgen. Keine automatische Wiederholung.'
 }
} catch {Log ('Steuerungsfehler: '+$_.Exception.Message);exit 1}
finally {if($locked) {$mutex.ReleaseMutex()};$mutex.Dispose()}
