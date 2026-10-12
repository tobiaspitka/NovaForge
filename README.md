# NovaForge 1.3.0-beta.2 · Assistent für Einsteiger

Der neue Assistent erklärt jede Seite in Alltagssprache und führt mit dauerhaft sichtbaren Zurück-/Weiter-Schaltflächen durch sechs Schritte. Ohne Zusatzprogramme wird die Aufnahme übersprungen. Ungeprüfte Programme sperren weiterhin die Fortsetzung zur ISO-Erstellung.

Windows-Datei und USB-Stick haben eigene Auswahlkarten. Vor der Erstellung zeigt eine Checkliste Quelle, Programme, Microsoft-ISO-Werkzeug und neuen Speicherort. Das Werkzeug muss einmalig aus den Microsoft ADK Deployment Tools eingerichtet werden. Erweiterte Pfade und Protokolle sind aufklappbar. Der Recorder erklärt die Auswahl eines installierten Programms als Erfolgsnachweis; zusätzliche Aktionen bleiben verfügbar.

Die Oberfläche nutzt abgerundete Karten, dezente Verläufe, besser lesbare Kontraste und eine sichtbare Tastaturfokussierung. Bestehende Projekte und Aufnahmen bleiben erhalten. Lokale Versionsstände werden nach erfolgreichen Prüfungen gespeichert.

Geprüft werden echte WPF-Seiten, Assistenten-Sperren, Navigation ohne Software, bestehende Aufnahme-/Paketfunktionen und der eigenständige EXE-Start. Der vollständige Sandbox-/VNC-Treiber-/ISO-Ablauf bleibt ungeprüft, da Sandbox auf diesem Gerät deaktiviert ist.

# NovaForge 1.3 Beta · Geführte Softwarevorbereitung

Jede aktive Software durchläuft einen verständlichen Ablauf: **Installation vormachen → Aufnahme abschließen → in einer neuen Sandbox testen → für ISO freigegeben**. Die ISO-Erstellung bleibt gesperrt, solange ein aktives Programm noch nicht vollständig geprüft ist. Vorhandene Projekte und Aufnahmen bleiben nutzbar; alte Silent-Konfigurationen müssen für diesen geführten Ablauf aufgenommen werden.

Unter Programme einen Installer hinzufügen und den Programmnamen prüfen. Bei EXE-Dateien wird nach Möglichkeit der Produktname gelesen; der Anzeigename kann frei geändert werden. Der Kopfbereich und die dunkle Titelleiste zeigen das aktuelle Programm. Treiber-/Begleitordner und technische Optionen sind aufklappbar.

Im Installationsstudio zeigt die zentrale Schaltfläche den nächsten erforderlichen Schritt. Windows Sandbox kann dort vorbereitet werden. Die erste Sitzung installiert und zeichnet auf. Aufnahme mit installiertem Prüfnachweis speichern und schließen. Nach Hostübernahme und geschlossener Sandbox folgt der Test in einer neuen Sitzung. NovaForge verhindert den Start einer zweiten Aufnahme-/Testsitzung, solange die vorherige Sandbox geöffnet ist.

Nach erfolgreichem Test ist das Programm als „Für ISO geprüft“ markiert. „Nächstes Programm“ führt zur nächsten unvollständigen Software. Eine Änderung des Installerpfads, Hashes, Begleitordners oder Ablaufs hebt die Freigabe auf. Reine Anzeigenamen ändern die geprüfte Ausführung nicht. Gespeicherte Aufnahmen bleiben in der Bibliothek verfügbar.

Auch das Sandbox-Fenster hat jetzt einen Programmkopf, verständliche Schritte, einen eigenen Aufnahmenamen, aufklappbare Hilfe und getrennte Aktionen für Entwurf und Abschluss. Die Aufnahme bleibt ein Entwurf, solange Schritte oder Prüfdatei fehlen. Ein erfolgreicher unveränderter Test ist für die ISO verpflichtend.

Windows Sandbox und die vollständige VNC-/Treiber-/ISO-Installation wurden hier nicht ausgeführt. UI, Statusübergänge, Freigabesperre und vorhandene Automations-/Paketfunktionen werden geprüft. UAC bleibt manuell; hardwareabhängige Treiber können in Sandbox scheitern. Vertrauliche Werte bleiben im Profil und auf der ISO im Klartext.
# NovaForge 1.2.0-beta.6 · Aufnahme- und Speicherfix

- Aufnahme erfasst UIA-Ereignisse und beobachtet Klicks über vorab gelesene Selektoren. Keine UIA-/Dateiprüfung im zeitkritischen Eingabe-Hook. Aufnahmefehler werden angezeigt und protokolliert.
- Für zusätzliche Windows-Treiberbestätigungen (z. B. beim VNC-Bildschirmtreiber) wird der Prozesspfad mit begrenzten Abfragerechten statt über Modulzugriff ermittelt. Der Dialog wird als eigenes Fenster erfasst; ein echter VNC-/Treiberlauf wurde hier nicht durchgeführt.
- Installer wird im Gast automatisch gestartet; zusätzliche Installations-/Treiberfenster werden standardmäßig mit erfasst. Über den Schalter kann dies abgeschaltet werden. UAC auf dem geschützten Desktop bleibt manuell.
- Aufnahme jederzeit als Entwurf speichern, auch bei laufendem Installer oder fehlender Prüfdatei. Entwürfe bleiben in der Bibliothek, benötigen vor ISO-Nutzung aber einen erfolgreichen frischen Test.
- Namen im Host und Gast frei vergeben. Gespeicherte Aufnahmen über „Name / Prüfdatei bearbeiten“ ändern und mit selbst gewähltem Dateinamen exportieren.
- „Speichern und schließen“ wartet auf die Bestätigung des Hosts. Derselbe Sitzungsordner kann mehrere aktualisierte Profilstände übertragen; veraltete Bestätigungen werden nicht akzeptiert.
- Begleitordner mit Installer, Treiberdateien und Unterordnern auswählen. Originaldateiname und Verzeichnisstruktur bleiben in Sandbox und ISO erhalten. Paketinhalte sind prüfsummengebunden. Im Gast und bei Wiedergabe wird eine beschreibbare Arbeitskopie verwendet.
- Neu installierte Dateien unter Windows/System32/drivers sind als Prüfnachweis möglich. Ordnerpfade innerhalb eines Installerpakets werden für die Wiedergabe angepasst.

In der Sandbox F7 für eine fokussierte Aktion verwenden, falls ein Anbieter kein UI-Ereignis liefert. F8 übernimmt Text, F9 vertrauliche Werte. Vertrauliche Profile und ISO-Medien enthalten diese Werte im Klartext.

Treiber, die echte Hardware, einen Kernelneustart oder nicht verfügbare Geräte benötigen, können in Windows Sandbox scheitern. Für diese Fälle ist eine passende Test-VM bzw. Testhardware erforderlich. Die Beta umgeht diese Windows-Sandbox-Grenzen nicht. Die vollständige Sandbox-/ISO-Abnahme wurde auf diesem Gerät nicht durchgeführt; genaue Prüfgrenzen stehen im Testprotokoll.
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

Windows-Prozessabfrage: [Microsoft QueryFullProcessImageName](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew).
