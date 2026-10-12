param([string]$ReportPath=(Join-Path $PSScriptRoot 'runner-results.txt'))
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('NovaForge-RunnerTest Ä mit Leerzeichen-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$oldWindir=$env:windir;$oldLocal=$env:LOCALAPPDATA;$oldProgram=$env:ProgramData;$oldMarker=$env:NOVAFORGE_TEST_MARKER
$script:passed=0;$script:lines=@()
function Assert($value,[string]$name) {if(!$value){throw ('FAIL '+$name)};$script:passed++;$script:lines+=('PASS '+$name);Write-Output ('PASS '+$name)}
function New-App([string]$name,[string]$arguments,[string]$context='System',[string[]]$deps=@()) {
 $id=[Guid]::NewGuid().ToString('N');$target=Join-Path $payload ('Installers\'+$id+'.exe');Copy-Item -LiteralPath $fixture -Destination $target
 return [PSCustomObject]@{Id=$id;Name=$name;Path=$name+'.exe';Type='EXE';Arguments=$arguments;Context=$context;TimeoutSeconds=2;Sha256=(Get-FileHash -LiteralPath $target).Hash;RebootPolicy='PauseUntilReboot';DependencyIds=$deps;AllowedCodes=@(0,3010)}
}
function Set-Manifest($apps) {[IO.File]::WriteAllText((Join-Path $payload 'manifest.json'),(@{Version=1;BuildId='test';Programs=@($apps)}|ConvertTo-Json -Depth 10))}
function State {[IO.File]::ReadAllText((Join-Path $payload 'State\state.json'))|ConvertFrom-Json}
try {
 $fixture=Join-Path $testRoot 'fixture.exe'
 $compiler=Join-Path $oldWindir 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 & $compiler /nologo /target:exe ("/out:"+$fixture) (Join-Path $PSScriptRoot 'Fixture.cs')
 if($LASTEXITCODE -ne 0){throw 'Fixture-Kompilierung fehlgeschlagen.'}
 $env:windir=Join-Path $testRoot 'Windows';$env:LOCALAPPDATA=Join-Path $testRoot 'Local';$env:ProgramData=Join-Path $testRoot 'ProgramData'
 $payload=Join-Path $env:windir 'NovaForge';New-Item -ItemType Directory -Force (Join-Path $payload 'Installers'),(Join-Path $env:ProgramData 'NovaForge\UserCompletion') | Out-Null
 $runner=Join-Path $projectRoot 'scripts\Runner.ps1'
 $env:NOVAFORGE_TEST_MARKER=Join-Path $testRoot 'executed.txt'
 $a=New-App 'Erfolg mit Umlaut Ä' '0';$b=New-App 'Neustart' '3010' 'System' @($a.Id);$c=New-App 'Nach Neustart' '0' 'System' @($b.Id)
 Set-Manifest @($a,$b,$c)
 & $runner -Context System -SetupPhase
 $s=State;Assert ($s.($a.Id).Status -eq 'Success') 'Real process success';Assert ($s.($b.Id).Status -eq 'RebootRequired') 'Real exitcode 3010';Assert ($null -eq $s.($c.Id)) 'Reboot pauses queue'
 & $runner -Context System -SetupPhase
 Assert ((Get-Content -LiteralPath $env:NOVAFORGE_TEST_MARKER).Count -eq 2) 'Same-boot resume never repeats completed installers'
 $s._reboot='previous boot';[IO.File]::WriteAllText((Join-Path $payload 'State\state.json'),($s|ConvertTo-Json -Depth 10))
 & $runner -Context System -SetupPhase
 $s=State;Assert ($s.($c.Id).Status -eq 'Success') 'Queue resumes after boot marker changes'
 $bad=New-App 'Fehler' '5';$dep=New-App 'Abhängig' '0' 'System' @($bad.Id);$timeout=New-App 'Zeitlimit' '0 10000';$tamper=New-App 'Prüfsumme' '0'
 Add-Content -LiteralPath (Join-Path $payload ('Installers\'+$tamper.Id+'.exe')) -Value 'tampered'
 Set-Manifest @($bad,$dep,$timeout,$tamper)
 & $runner -Context System -SetupPhase
 $s=State;Assert ($s.($bad.Id).Status -eq 'Failed') 'Disallowed exitcode stored';Assert ($s.($dep.Id).Status -eq 'Blocked') 'Failed dependency blocks installer';Assert ($s.($timeout.Id).Status -eq 'Timeout') 'Timeout terminates real process';Assert ($s.($tamper.Id).Status -eq 'Failed') 'Payload hash mismatch prevents execution'
 $interrupted=New-App 'Unterbrochen' '0';$s|Add-Member -NotePropertyName $interrupted.Id -NotePropertyValue @{Status='Running'}
 [IO.File]::WriteAllText((Join-Path $payload 'State\state.json'),($s|ConvertTo-Json -Depth 10));Set-Manifest @($interrupted)
 & $runner -Context System -SetupPhase
 $s=State;Assert ($s.($interrupted.Id).Status -eq 'Interrupted') 'Running state becomes interrupted without re-execution'
 $user=New-App 'Benutzer' '0' 'User' @($a.Id);Set-Manifest @($a,$user)
 & $runner -Context User
 $userState=Get-Content -LiteralPath (Join-Path $env:LOCALAPPDATA 'NovaForge\state.json') -Raw -Encoding UTF8|ConvertFrom-Json
 Assert ($userState.($user.Id).Status -eq 'Success') 'User queue reads system dependency and writes per-user state'
 Assert (Test-Path -LiteralPath (Join-Path $env:ProgramData 'NovaForge\UserCompletion\test.done')) 'Completion marker written'
 $pending=New-App 'System läuft noch' '0';$waitingUser=New-App 'Wartender Benutzer' '0' 'User' @($pending.Id)
 $s=State;$s | Add-Member -NotePropertyName $pending.Id -NotePropertyValue @{Status='Running'}
 $systemStatePath=Join-Path $payload 'State\state.json';[IO.File]::WriteAllText($systemStatePath,($s|ConvertTo-Json -Depth 10));Set-Manifest @($pending,$waitingUser)
 $job=Start-Job -ScriptBlock {param($file,$id);Start-Sleep -Seconds 2;$data=Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json;$data.$id.Status='Success';$tmp=$file+'.job';[IO.File]::WriteAllText($tmp,($data|ConvertTo-Json -Depth 10));[IO.File]::Replace($tmp,$file,$file+'.jobbackup');[IO.File]::Delete($file+'.jobbackup')} -ArgumentList $systemStatePath,$pending.Id
 try {& $runner -Context User;Wait-Job $job | Out-Null;Receive-Job $job | Out-Null} finally {Remove-Job $job -Force}
 $userState=Get-Content -LiteralPath (Join-Path $env:LOCALAPPDATA 'NovaForge\state.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 Assert ($userState.($waitingUser.Id).Status -eq 'Success') 'User waits for running system dependency and resumes during same logon'
 $msi=New-App 'MSI Parameter' '';$msi.Type='MSI';$msi.Path='fixture.msi'
 Move-Item -LiteralPath (Join-Path $payload ('Installers\'+$msi.Id+'.exe')) -Destination (Join-Path $payload ('Installers\'+$msi.Id+'.msi'))
 New-Item -ItemType Directory -Force (Join-Path $env:windir 'System32') | Out-Null
 Copy-Item -LiteralPath $fixture -Destination (Join-Path $env:windir 'System32\msiexec.exe')
 Set-Manifest @($msi);& $runner -Context System -SetupPhase
 $argsLogged=(Get-Content -LiteralPath $env:NOVAFORGE_TEST_MARKER)[-1]
 $s=State;Assert ($s.($msi.Id).Status -eq 'Success') 'MSI substitute exits successfully'
 Assert ($argsLogged -match '/qn' -and $argsLogged -match '/norestart' -and $argsLogged -match 'REBOOT=ReallySuppress' -and $argsLogged -match 'ALLUSERS=1' -and $argsLogged -match '/L\*v') 'Actual MSI argument construction includes quiet, restart suppression, log and context'
 $packed=New-App 'Paket Erfolg' '0';$broken=New-App 'Paket geändert' '0'
 foreach($app in @($packed,$broken)){
  $dir=Join-Path $payload ('Installers\'+$app.Id);New-Item -ItemType Directory -Force (Join-Path $dir 'Drivers') | Out-Null
  Move-Item -LiteralPath (Join-Path $payload ('Installers\'+$app.Id+'.exe')) -Destination (Join-Path $dir 'Original-Setup.exe')
  $sidecar=Join-Path $dir 'Drivers\fixture.inf';[IO.File]::WriteAllText($sidecar,'harmless test data')
  $app | Add-Member -NotePropertyName InstallerRelativePath -NotePropertyValue ($app.Id+'/Original-Setup.exe')
  $app | Add-Member -NotePropertyName PackageFiles -NotePropertyValue @([PSCustomObject]@{Path='Drivers\fixture.inf';Sha256=(Get-FileHash -LiteralPath $sidecar).Hash})
 }
 Add-Content -LiteralPath (Join-Path $payload ('Installers\'+$broken.Id+'\Drivers\fixture.inf')) -Value 'changed'
 Set-Manifest @($packed,$broken);& $runner -Context System -SetupPhase
 $s=State;Assert ($s.($packed.Id).Status -eq 'Success') 'Packaged installer executes with original filename and verified driver sidecar';Assert ($s.($broken.Id).Status -eq 'Failed') 'Modified driver sidecar blocks packaged installer'
 $script:lines+=($script:passed.ToString()+' integration tests passed. No application was installed.');$script:lines | Set-Content -LiteralPath $ReportPath -Encoding UTF8
} catch {
 if(Test-Path -LiteralPath $payload){Get-ChildItem -LiteralPath $payload -Recurse -File | Where-Object Extension -in '.log','.json' | ForEach-Object {Write-Output $_.FullName;Get-Content -LiteralPath $_.FullName}}
 throw
} finally {
 $env:windir=$oldWindir;$env:LOCALAPPDATA=$oldLocal;$env:ProgramData=$oldProgram;$env:NOVAFORGE_TEST_MARKER=$oldMarker
 $full=[IO.Path]::GetFullPath($testRoot);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath());if(!$full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsicherer Testbereinigungspfad.'};Remove-Item -LiteralPath $full -Recurse -Force
}
