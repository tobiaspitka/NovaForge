# NovaForge 1.2.0-beta.3 · Installationsstudio

Neue Beta mit Aufnahmebibliothek, automatischer Übernahme, Bereinigung und USB-/Ordnerquellen. Die stabile [Version 1.1.1](https://github.com/tobiaspitka/NovaForge/releases/tag/v1.1.1) bleibt verfügbar.

## Aufnahme und Wiederholung

1. Installer unter Programme auswählen und Installationsstudio öffnen. Falls erforderlich „Windows Sandbox aktivieren“ anklicken, Administratorfreigabe bestätigen und Windows anschließend manuell neu starten. Die Aktivierung setzt eine unterstützte Windows-Edition und Virtualisierung voraus.
2. Aufnahme starten und den Installer in der Sandbox bedienen. Recorder und Wiedergabe laufen mit Administratorrechten. Zugängliche Schaltflächen, Optionen und Auswahlfelder werden erfasst. Textfelder mit F8 aufnehmen. Für Lizenzschlüssel oder Kennwort das Zielfeld fokussieren, F9 drücken und den Wert ausdrücklich eingeben.
3. Zusätzliche Fenster außerhalb des ursprünglichen Installerprozesses über „Installerfenster verbinden“ auswählen, danach normal bedienen. Die Prozessdatei wird per SHA-256 gebunden; eine andere Version wird nicht automatisch akzeptiert. Windows-UAC auf dem geschützten Desktop bleibt manuell. Windows Sandbox unterstützt nicht jede Treiber-/Hardwareinstallation; kein universeller Treiber-Test zugesichert.
4. Eine neu installierte Programmdatei als Erfolgskriterium wählen, „Speichern & Sandbox schließen“ drücken. Die Sandbox beendet sich; NovaForge übernimmt das Profil automatisch in „Meine Aufnahmen“ und ordnet es dem passenden Installer zu.
5. „Aufnahme frisch testen“ starten. Das erfolgreich getestete Profil wird automatisch übernommen und kann für die ISO genutzt werden. Bereits gespeicherte Aufnahmen lassen sich in der Bibliothek auswählen und einem passenden Installer zuordnen.

Die Bibliothek liegt unter `%LOCALAPPDATA%\NovaForge\Beta\Recordings`. Große Installer-/EXE-Kopien in RecorderSessions werden erst nach gesicherter Übernahme und beendeter Sandbox gelöscht. Fehlermeldungen bleiben als kleine Diagnoseprotokolle erhalten. Abgebrochene Sitzungen ohne Profil werden nach Schließen der Sandbox und einer Wartezeit bereinigt. Nicht übernommene Profile werden nicht gelöscht. Ist NovaForge geschlossen, erfolgt Übernahme und Bereinigung beim nächsten Start.

**Vertrauliche Schritte enthalten Lizenzschlüssel oder Kennwörter im Klartext.** Sie liegen im Aufnahmeprofil und auf der erzeugten ISO. Listen und normale Wiedergabeprotokolle zeigen die Werte nicht an. Bibliothek, ISO und USB-Medien nur berechtigten Personen zugänglich machen. Lokale Windows-Kontokennwörter aus den Setup-Optionen werden weiterhin nicht im Projekt gespeichert.

Eingabeordner sind schreibgeschützt, nur der separate Ausgabeordner ist beschreibbar. Netzwerk ist standardmäßig deaktiviert; die Webinstaller-Option aktiviert es. Zwischenablage ist deaktiviert. Die veröffentlichte portable Beta-EXE ist erforderlich. Profile sind an Installer-Prüfsumme und unveränderten getesteten Ablauf gebunden. Bei fehlenden oder mehrdeutigen Steuerelementen stoppt die Wiedergabe. Aufnahmen aus älteren Betas gegebenenfalls erneut testen.

## USB-Stick oder entpacktes Medium → ISO

Unter Windows-Medium „USB / Medienordner auswählen“ anklicken und den Stammordner mit setup.exe, sources und Bootdateien wählen. Die Quelle wird nur gelesen und zunächst bytegetreu in einen temporären Arbeitsordner kopiert. Ausgabe und Arbeitsverzeichnis außerhalb der Quelle wählen; am besten auf einem anderen Laufwerk. Auch geteilte install.swm-Abbilder werden unterstützt. Microsoft oscdimg aus den ADK Deployment Tools bleibt erforderlich.

Ohne aktivierte Programme oder Setup-Anpassungen wird das vorhandene Medium einschließlich vorhandener Antwortdateien/OEM-Anpassungen wieder zur ISO. Bei neuen Anpassungen werden bestehende Antwortdateien/OEM-Anpassungen als Konflikt gemeldet. Ventoy-Sticks, einzelne ISO-Dateien ohne entpacktes Windows-Setup und reine Wiederherstellungsmedien sind keine unterstützten Ordnerquellen. Fehlende Bootdateien werden nicht erfunden.

Windows Sandbox und echte Drittanbieter-Installer wurden hier nicht ausgeführt. Die UI-Automation wurde mit der eigenen harmlosen Testanwendung geprüft; Ordnerkopie, Hashprüfung und Bereinigungsgrenzen mit Testdateien. Eine vollständige Windows-Installation und USB→ISO mit einem echten Windows-Medium wurden mangels Testmedium/oscdimg nicht geprüft.
# NovaForge

Eine lokale Windows-Desktop-Anwendung zum Erstellen eigener Windows-10/11-x64-Installationsmedien mit Programmen und wählbaren Setup-Einstellungen.

[Versionen und EXE-Downloads](https://github.com/tobiaspitka/NovaForge/releases) · [Änderungen](CHANGELOG.md) · [Architektur](ARCHITEKTUR.md) · [Testprotokoll](TESTPROTOKOLL.md)

## Start und Voraussetzungen

Bei GitHub Releases die gewünschte NovaForge-<Version>.exe herunterladen und starten. Das Portable-ZIP enthält zusätzlich Dokumentation und Lizenztexte. Frühere Versionen stehen getrennt zur Verfügung. Die EXE enthält .NET 10 LTS und native WPF-Komponenten; keine separate .NET-Installation erforderlich. Native Bibliotheken werden unter %TEMP%\.net extrahiert. Die EXE ist nicht digital signiert.

NovaForge arbeitet lokal ohne Benutzerkonto, Webserver oder kostenpflichtigen Dienst. Die ursprüngliche ISO bleibt erhalten. Hinzugefügte Installer werden auf dem Erstellungscomputer weder installiert noch ausgeführt; Aufnahmen erfolgen in Windows Sandbox.

Für ISO-Erstellung werden Windows 10/11 x64, eine originale Windows-x64-ISO, integriertes DISM/Windows PowerShell 5.1 und Microsoft oscdimg aus den **Windows ADK Deployment Tools** benötigt. [Offizielle ADK-Anleitung und Download](https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install). NovaForge erkennt den Standardpfad und erlaubt manuelle Auswahl. oscdimg wird nicht mitgeliefert; das separate WinPE-Add-on ist hier nicht nötig.

Ausreichend Platz für extrahierte ISO, Installer und neue ISO einplanen. Bei gemeinsamem Laufwerk grob zweimal ISO plus Installer und mindestens 1 GiB Reserve. Administratorrechte werden nur für Mount/Kopie und Ausgabeprüfung angefordert; die Oberfläche und oscdimg laufen ohne allgemeine Erhöhung.

## Bedienung

1. **Windows-ISO:** Original-ISO auswählen oder ins Fenster ziehen; oscdimg zuordnen.
2. **Programme:** MSI-/EXE-Dateien hinzufügen und Name, Silent-Parameter, Kontext, Reihenfolge, Abhängigkeiten, Zeitlimit, Exitcodes und Neustartbehandlung konfigurieren. Systemprogramme laufen vor Benutzerprogrammen; innerhalb einer Phase gehen Abhängigkeiten vor Listenreihenfolge.
3. **Setup-Optionen:** Gewünschte Anpassungen einzeln aktivieren. Ohne aktivierten Schalter bleibt die jeweilige Windows-Vorgabe erhalten. Auch ein Projekt ausschließlich mit Setup-Anpassungen ist möglich.
4. **Erstellung:** Neuen Ausgabepfad und Arbeitsverzeichnis wählen. Vorhandene ISO-Ausgaben werden nicht überschrieben. Kopie/Dateiprüfung zeigen gemessenen Fortschritt; Hashen, DISM und oscdimg eine unbestimmte Anzeige mit echtem Protokoll. Abbruch gibt eigene Mounts und temporäre Ressourcen kontrolliert frei.
5. **Ergebnis:** ISO, Größe, SHA-256 und Programme prüfen. Neben der ISO liegen .sha256 und .log; Ordner und Protokoll lassen sich direkt öffnen.

Native Datei-/Ordnerdialoge folgen dem Windows-Design. Die Anwendung hat ein dunkles Design, Größenänderung, DPI-Unterstützung und scrollbare Eigenschaften.

## Setup-Optionen

| Option | Umsetzung und Grenze |
|---|---|
| Lokaler Benutzer | oobeSystem/LocalAccounts mit Kontoname, Kennwort und wählbarer Administratorrolle. Kein automatisches Login. |
| „Ich habe kein Internet“ | BypassNRO vor OOBE. **Buildabhängig**; auf neueren Windows-11-Builds kann die Schaltfläche fehlen. |
| TPM-Prüfung überspringen | Experimentelles LabConfig/BypassTPMCheck im windowsPE-Setup. |
| Secure-Boot-Prüfung überspringen | Experimentelles LabConfig/BypassSecureBootCheck; Firmware-Secure-Boot wird nicht ausgeschaltet. |
| RAM-Prüfung überspringen | Experimentelles LabConfig/BypassRAMCheck. |
| Computername | specialize-Antwortdatei; 1–15 gültige ASCII-Zeichen. |
| Region/Tastatur | Deutschland, Österreich oder USA. Anzeigesprache der Original-ISO bleibt erhalten. |
| Zeitzone | Wien/Berlin, London, UTC oder New York. |
| OEM-Registrierung | Dokumentiertes HideOEMRegistrationScreen. |
| Lange Pfade | LongPathsEnabled auf dem Zielsystem; Anwendungen müssen Langpfade unterstützen. |

Alle Optionen betreffen ausschließlich die neue ISO und deren Zielinstallation. Windows Update, Defender und Aktivierung werden nicht verändert; keine Aktivierungsumgehung. CPU-Instruktionsanforderungen wie SSE4.2/POPCNT bleiben bestehen. Installationen auf ungeeigneter Hardware sind nicht von Microsoft unterstützt und müssen mit der konkreten ISO geprüft werden.

**Lokale Windows-Kontokennwörter werden nicht in Projekten oder Protokollen gespeichert.** Nach Projektöffnung erneut eingeben. Die ISO-Antwortdatei benötigt das Kennwort: Windows-SIM-Base64 ist Kodierung, keine Verschlüsselung. Die ISO vertraulich behandeln. [Microsoft: lokale Konten](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-useraccounts-localaccounts-localaccount), [sprachunabhängige Kontengruppen](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-useraccounts-localaccounts-localaccount-group).

Lokale Konten über Antwortdateien sind dokumentiert. BypassNRO und LabConfig sind keine allgemein garantierten Microsoft-Setup-APIs. Referenz für das Verfahren: [Rufus-Implementierung](https://github.com/pbatard/rufus/blob/master/src/wue.c). Rufus-Code und -Werkzeuge werden nicht mitgeliefert. [Microsoft: Entfernung bekannter Offline-OOBE-Mechanismen](https://blogs.windows.com/windows-insider/2025/10/06/announcing-windows-11-insider-preview-build-26220-6772-dev-channel/).

## Installer und Zielsteuerung

MSI erhält /qn /norestart REBOOT=ReallySuppress /L*v, zitierte Installer-/Logpfade und Kontextparameter: ALLUSERS=1 für System, ALLUSERS=2 MSIINSTALLPERUSER=1 für Benutzer. Das MSI muss den gewählten Kontext unterstützen.

EXE-Dateien benötigen geprüfte Silent-Parameter. Inno-Setup-, NSIS- und InstallShield-MSI-Wrapper-Profile sind editierbare Vorschläge; universelle EXE-Parameter gibt es nicht. Herstellerparameter und Neustartunterdrückung prüfen und bestätigen. Vollständige Offline-Installer erlauben Offline-Einrichtung. Bekannte Webinstaller markieren; sie benötigen Internet auf dem Zielcomputer. NovaForge lädt keine Installer nach.

Windows Setup kopiert sources\$OEM$\$$\NovaForge nach %WINDIR%\NovaForge. Die Antwortdatei startet Bootstrap synchron in **specialize unter SYSTEM**, schützt den Payload per ACL und registriert Fortsetzung. SetupComplete.cmd wird nicht benötigt. [Microsoft: RunSynchronous](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-deployment-runsynchronous), [SetupComplete-OEM-/Neustartgrenzen](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/add-a-custom-script-to-windows-setup).

Systeminstaller müssen ohne interaktiven Desktop funktionieren. NovaForge-System setzt bei Start/Anmeldung und alle fünf Minuten fort und wartet während SystemSetupInProgress. NovaForge-User läuft bei Anmeldung ohne Erhöhung und wartet im Hintergrund auf den Abschluss der Systemwarteschlange. Diese Version richtet Benutzerprogramme für das erste erfolgreich installierte Konto ein; gleichzeitige Mehrbenutzeranmeldungen sind nicht abgenommen.

Installer laufen nacheinander ohne cmd-/Shell-Auswertung ihrer Parameter. SHA-256 wird vor Ausführung geprüft, Zustand atomar vor/nach dem Prozess gespeichert. Erfolg wird nicht wiederholt. Exitcode 3010 pausiert standardmäßig bis zu einem **manuellen Neustart nach Setup**. 1641 gilt als unerwarteter Neustart. Fehler, Timeout, Interrupted und abhängige Blocked-Einträge bleiben ohne blinde Wiederholung protokolliert. NovaForge startet Windows nicht während Setup neu. Nach vollständigem Erfolg werden Startaufgaben entfernt; Status und Protokolle bleiben.

## ISO-Verarbeitung und Projekte

Die Quelle wird read-only eingebunden. DISM prüft boot.wim auf Windows 10.x/x64. Alle Dateien werden kopiert; WIM-/ESD-Header sowie SWM-GUID, Teilnummern und Vollständigkeit werden geprüft. Alle Editionen und Abbilder bleiben erhalten, ohne Recompression. Externe Antwortdateien, OEM-Verzeichnisse und Setup-Skripte führen zu einem Konflikt; Anpassungen innerhalb unverändert erhaltener WIM/ESD-Abbilder werden nicht analysiert.

oscdimg erzeugt UDF 1.02 mit BIOS-/UEFI-Bootdaten, soweit vorhanden. Die Ausgabe wird erneut eingebunden; alle Dateigrößen und SHA-256 sowie Antwortdatei und El-Torito-Bootkatalog werden geprüft. Erst dann wird der endgültige Dateiname übernommen. [Microsoft: oscdimg](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/oscdimg-command-line-options).

Projektformat **3** speichert Dateien, Hashes, Programme, Reihenfolge, Abhängigkeiten und Setup-/Ausgabeoptionen. Version-1-Projekte werden migriert. Relative Pfade sind relativ zur Projektdatei. Fehlende/geänderte Dateien ausdrücklich neu zuordnen; Lokale Windows-Kontokennwörter bleiben aus Projekt-JSON ausgeschlossen; vertrauliche Recorder-Schritte sind im separaten Profil enthalten. [Quelloffenes Beispiel](examples/README.md).

## Versionen und Entwicklung

Nach erfolgreichem Build, Kern-/Prozesstests, UI-Layout und portabler EXE-Startprüfung wird jeder veröffentlichte Entwicklungsstand unter **%LOCALAPPDATA%\NovaForge\Versions\<Version>** gesichert: EXE, Quellcode-ZIP, SHA-256, Zeitstempel und Testnachweise. Vorherige Versionen bleiben erhalten. Das Setup-Menü öffnet diesen Ordner.

Ensure-Version.ps1 vergleicht vor Publish den Quellstand mit der bereits gesicherten aktuellen Version und erhöht bei Änderungen automatisch die Patchnummer. Minor-/Major-Versionen können über Version in src/NovaForge/NovaForge.csproj festgelegt werden. Store-Version.ps1 verweigert Sicherungen ohne passende Test-/Startnachweise oder nach ungetesteten Quelländerungen. „Geprüft“ bezeichnet die ausgewiesenen Prüfungen, keine erfundene VM-Abnahme.

.NET SDK gemäß global.json unter Windows installieren. Keine Drittanbieter-NuGet-Pakete erforderlich; Runtime-Pakete werden beim ersten Publish bezogen.

```powershell
.\scripts\Build.ps1
.\scripts\Publish.ps1
.\scripts\Package.ps1
# Optional eigener SDK-Pfad:
.\scripts\Publish.ps1 -Dotnet C:\SDK\dotnet.exe
```

Publish verwendet win-x64, self-contained, Single File, native Selbstextraktion und Kompression, kein Trimming und keine externen PDBs. Lizenztexte werden beigelegt. Package erzeugt versionsbenannte EXE-/ZIP-Dateien und Prüfsummen. SDKs, Builds, ISOs, Zugangsdaten, Arbeitsprotokolle und lokale Versionsarchive gehören nicht ins Git-Repository.

GitHub Actions baut auf Windows bei Push/PR. Tags v<Version> erzeugen Releases mit EXE, Portable-ZIP, Quellcode-ZIP und Prüfsummen. Tag und Projektversion müssen übereinstimmen. Bestehende Releases werden nicht überschrieben.

## Fehlersuche und Prüfgrenzen

Build-Protokoll: <Ausgabe>.iso.log. Systemstatus/-logs: %WINDIR%\NovaForge\State. Benutzerstatus/-logs: %LOCALAPPDATA%\NovaForge. Bootstrapfehler: %WINDIR%\NovaForge\bootstrap-error.log. Windows Setup: %WINDIR%\Panther\UnattendGC\setupact.log.

Bei Hashfehlern nur beabsichtigt aktualisierte Dateien neu zuordnen. Bei 3010 nach Setup neu starten. Fehler/Interrupted zunächst anhand installierter Produkte und Protokolle prüfen. Für gezielte Wiederholung Aufgaben deaktivieren, nur betreffende Fehler- und abhängige Blocked-Einträge aus state.json entfernen, Aufgaben reaktivieren. Erfolgreiche Einträge erhalten und niemals eine laufende Warteschlange bearbeiten.

**Prüfstand:** Build, Kernlogik, echte harmlose Prozesse, UI-Layout und EXE-Start wurden unter Windows geprüft. Ohne bereitgestellte Original-ISO, oscdimg und Test-VM sind vollständige ADK-ISO-Erstellung, Boot, Windows-Neuinstallation und Hardware-/OOBE-/Konteneffekte noch nicht praktisch abgenommen. Experimentelle Schalter werden nicht als universell garantiert bezeichnet. [Details und VM-Testplan](TESTPROTOKOLL.md).
