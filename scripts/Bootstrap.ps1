$ErrorActionPreference='Stop'
$root=Join-Path $env:windir 'NovaForge'
try {
 # Payload cannot be modified by ordinary users before a SYSTEM task executes it.
 & icacls.exe $root /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-32-545:(OI)(CI)RX' /T /Q | Out-Null
 if($LASTEXITCODE -ne 0) { throw 'Payload-Berechtigungen konnten nicht gesetzt werden.' }
 $completion=Join-Path $env:ProgramData 'NovaForge\UserCompletion'
 New-Item -ItemType Directory -Force $completion | Out-Null
 & icacls.exe $completion /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-32-545:(OI)(CI)M' /Q | Out-Null
 if($LASTEXITCODE -ne 0) { throw 'Status-Berechtigungen konnten nicht gesetzt werden.' }
 $ps=Join-Path $env:windir 'System32\WindowsPowerShell\v1.0\powershell.exe'
 $settings=New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Days 7) -MultipleInstances IgnoreNew -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
 $action=New-ScheduledTaskAction -Execute $ps -Argument ('-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "'+$root+'\Runner.ps1" -Context System')
 $principal=New-ScheduledTaskPrincipal -UserId 'S-1-5-18' -LogonType ServiceAccount -RunLevel Highest
 $triggers=@((New-ScheduledTaskTrigger -AtStartup),(New-ScheduledTaskTrigger -AtLogOn),(New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 5)))
 Register-ScheduledTask -TaskName 'NovaForge-System' -Action $action -Principal $principal -Trigger $triggers -Settings $settings -Force | Out-Null
 $manifest=Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
 if(@($manifest.Programs | Where-Object Context -eq 'User').Count -gt 0) {
  $userAction=New-ScheduledTaskAction -Execute $ps -Argument ('-NoProfile -ExecutionPolicy Bypass -File "'+$root+'\Runner.ps1" -Context User')
  $userPrincipal=New-ScheduledTaskPrincipal -GroupId 'S-1-5-32-545' -RunLevel Limited
  Register-ScheduledTask -TaskName 'NovaForge-User' -Action $userAction -Principal $userPrincipal -Trigger (New-ScheduledTaskTrigger -AtLogOn) -Settings $settings -Force | Out-Null
 }
 & (Join-Path $root 'Runner.ps1') -Context System -SetupPhase
} catch {
 $_ | Out-String | Add-Content -LiteralPath (Join-Path $root 'bootstrap-error.log')
 exit 1
}
