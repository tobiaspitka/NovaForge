# NovaForge

NovaForge erstellt lokal aus einer Windows-10/11-x64-ISO ein neues Installationsmedium mit eigenen MSI-/EXE-Installern und einer dauerhaften Installationswarteschlange. Die originale ISO bleibt unverändert. Hinzugefügte Installer werden auf dem Erstellungscomputer weder installiert noch ausgeführt.

**Start:** `dist\win-x64\NovaForge.exe` doppelklicken. Die EXE ist tatsächlich für Windows x64 kompiliert und enthält .NET 10.0.12 einschließlich nativer WPF-Komponenten. Es ist keine separate .NET-Installation erforderlich. Native Bibliotheken werden beim ersten Start in den benutzereigenen .NET-Extraktionsordner unter `%TEMP%\.net` entpackt; dort muss Schreibzugriff bestehen. Die Anwendung ist nicht digital signiert.

**Prüfstand:** Build, Kernlogik, echte Testprozesse, Wiederaufnahme und WPF-Fenster/Datenbindungen wurden unter Windows 11 tatsächlich geprüft. Ein vollständiger Lauf mit Original-ISO, oscdimg, Firmware-Boot und Windows-Neuinstallation ist hier nicht durchgeführt worden. Details: [TESTPROTOKOLL.md](TESTPROTOKOLL.md). Die Implementierung enthält den vollständigen Erstellungsweg; die ausstehende VM-Abnahme ist keine behauptete erfolgreiche Installation.

## Voraussetzungen

- Windows 10 22H2 x64 oder Windows 11 x64 mit Windows PowerShell 5.1, Storage-/ScheduledTasks-Komponenten und integriertem DISM. Zielinstallation im normalen Desktopmodus; S-Modus und Audit-Modus sind nicht Gegenstand dieser Version.
- Eine originale, gültige Windows-10/11-x64-ISO und passende Windows-Lizenz. Andere Architekturen, WinPE-only-Medien und bereits angepasste OEM-Medien werden abgelehnt.
- Microsoft `oscdimg.exe` aus den **Deployment Tools** des Windows ADK. NovaForge erkennt den üblichen ADK-Pfad sowie PATH und bietet eine manuelle Auswahl. [Offizieller Download und Installationsanleitung](https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install). Das separate WinPE-Add-on wird für diesen Ablauf nicht benötigt: Bootdaten stammen aus der Quell-ISO. oscdimg wird nicht mitgeliefert; seine Lizenzbedingungen bleiben bei Microsoft.
- Platz für extrahierten ISO-Inhalt, alle Installer, neue ISO und Reserve. Bei gemeinsamem Laufwerk grob das Zweifache von ISO plus Installern und mindestens 1 GiB Reserve. Das tatsächliche Kopiervolumen wird zusätzlich geprüft.
- Ein Administratorkonto für die UAC-Freigaben des Mount-/Kopierhilfsprozesses und der Ausgabeprüfung. Die WPF-Anwendung und oscdimg laufen ohne allgemeine Administratoranforderung. Die Freigaben ändern keine Sicherheitseinstellungen.

## Bedienung

1. **Windows-ISO:** Original-ISO auswählen oder ins Fenster ziehen. ADK installieren beziehungsweise oscdimg zuordnen.
2. **Programme:** Lokale MSI-/EXE-Dateien hinzufügen. Anzeigename, Kontext, Parameter, Zeitlimit, erlaubte Exitcodes und Neustartbehandlung einstellen. Mit ↑/↓ priorisieren und Abhängigkeiten über die Auswahlliste setzen. Systemprogramme werden vor Benutzerprogrammen ausgeführt. Innerhalb dieser Phasen haben Abhängigkeiten Vorrang vor manueller Reihenfolge; unabhängige Programme behalten ihre Priorität. Deaktivierte Programme werden nicht integriert.
3. **Silent-Konfiguration prüfen:** MSI erhält automatisch `/qn /norestart REBOOT=ReallySuppress /L*v`, den Installerpfad und die Kontextparameter. Eigene Argumente können weitere MSI-Eigenschaften hinzufügen. Systemkontext verwendet `ALLUSERS=1`; Benutzerkontext `ALLUSERS=2 MSIINSTALLPERUSER=1` und benötigt ein dafür geeignetes MSI. EXE-Profile sind ausdrücklich Vorschläge für Inno Setup, NSIS und InstallShield-MSI-Wrapper. Es gibt keine universelle EXE-Konfiguration. Argumente und Neustartunterdrückung beim Hersteller prüfen und die Bestätigung setzen. Die Anwendung führt dafür keinen Installer-Test auf dem Host aus.
4. **Offline:** Vollständige Offline-Installer verwenden. Bekannte Webinstaller mit dem entsprechenden Kontrollkästchen kennzeichnen; sie benötigen während der Zielinstallation Internet. NovaForge kann einen Webinstaller nicht zuverlässig allein am Dateinamen erkennen und lädt keine Programme nach.
5. **Erstellung:** Neuen Ausgabepfad und Arbeitsverzeichnis auswählen. Bestehende ISO-Ausgaben werden abgelehnt. „Konfiguration prüfen“ prüft Dateien und Programmregeln, nicht den Inhalt einer noch nicht eingebundenen ISO. „ISO erstellen“ führt den gesamten Ablauf durch. Kopie und Dateiprüfung zeigen gemessenen Fortschritt; Hashen, DISM und oscdimg zeigen unbestimmten Fortschritt mit echtem Prozessprotokoll. Abbrechen beendet oscdimg mit seinem Prozessbaum beziehungsweise wartet auf kontrolliertes Freigeben des Mount-Hilfsprozesses.
6. **Ergebnis:** ISO, Größe, SHA-256 und effektive Reihenfolge erscheinen nach erfolgreicher Ausgabeprüfung. Neben der ISO liegen `.sha256` und `.log`. Ausgabeordner und Protokoll lassen sich direkt öffnen.

Native Windows-Dateiauswahldialoge folgen dem Betriebssystem-Design. Anwendungsdialoge, Karten und Auswahllisten verwenden das dunkle NovaForge-Design. Das Fenster unterstützt DPI-Skalierung und Größenänderung; bei kleinen Fenstern lässt sich der Programmeigenschaftenbereich scrollen.

## Ablauf auf dem Zielcomputer

Die ISO enthält `autounattend.xml` und `sources\$OEM$\$$\NovaForge`. Windows Setup kopiert den OEM-Inhalt nach `%WINDIR%\NovaForge`. Die Antwortdatei führt **einen** `Microsoft-Windows-Deployment/RunSynchronous`-Befehl in **specialize** unter SYSTEM aus. Dieser setzt die Payload-Berechtigungen, registriert die Fortsetzung und startet die systemweite Warteschlange synchron. Partitionierung, Kontoanlage, Sprache, Produktschlüssel und Editionsauswahl bleiben bei Windows Setup.

`SetupComplete.cmd` wird nicht benötigt. Microsoft dokumentiert seine Einschränkung bei OEM-Produktschlüsseln und verbietet dort Neustarts mit anschließender Skriptfortsetzung. Die gewählte specialize-Konfiguration nutzt `WillReboot=Never`; NovaForge ruft keinen Windows-Neustart auf. [Microsoft: Setup-Skripte](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/add-a-custom-script-to-windows-setup), [RunSynchronous](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-deployment-runsynchronous).

Systemprogramme müssen ohne interaktiven Desktop funktionieren und systemweite Installation unterstützen. Die geplante Aufgabe `NovaForge-System` läuft als SYSTEM bei Start/Anmeldung und zusätzlich alle fünf Minuten, solange eine Fortsetzung erforderlich ist. Sie wartet während `SystemSetupInProgress`. Benutzerprogramme laufen über `NovaForge-User` bei Anmeldung mit der Gruppe „Benutzer“ und niedrigstem Runlevel im interaktiven Konto. Damit wird keine Administratoranmeldung vorausgesetzt und keine UAC-Erhöhung für Benutzerprogramme erzeugt. Diese Version ist für **das erste erfolgreich installierte Benutzerkonto** bestimmt, nicht für die Verteilung an alle künftigen Profile. Gleichzeitige Mehrbenutzeranmeldungen sind nicht abgenommen.

Die Benutzerwarteschlange wartet im Hintergrund auf den Abschluss der Systemwarteschlange, damit beide Kontexte keine Installer parallel ausführen. Wird binnen 24 Stunden kein Abschluss erreicht, versucht sie die Fortsetzung bei der nächsten Anmeldung. Jeder Prozess wird einzeln gestartet; es gibt keine `cmd /c`-Auswertung der Installerargumente. NovaForge wartet bis zum Ende oder Zeitlimit, prüft Exitcodes und speichert Status vor und nach jedem Prozess atomar. SHA-256 wird auch auf dem Zielcomputer vor Ausführung kontrolliert. Der Payload-Ordner ist für normale Benutzer nur lesbar; SYSTEM und Administratoren dürfen ihn ändern. Benutzerzustand liegt separat im eigenen Profil.

- `Success`: zulässiger Exitcode, Installation abgeschlossen.
- `RebootRequired`: zulässiger Exitcode 3010. Standardmäßig Pause bis zu einem **tatsächlichen manuellen Neustart**. Ohne Neustart wird nicht weiterinstalliert. Eine Benutzerwarteschlange wartet ebenfalls auf einen offenen Systemneustart.
- `Failed`: unzulässiger Exitcode, fehlender Installer oder Prüfsummen-/Prozessfehler.
- `Timeout`: Prozessbaum nach überschrittenem Zeitlimit beendet.
- `Interrupted`: vorangegangener Status `Running` nach Abbruch beziehungsweise unerwartetem Neustart; kein automatischer Wiederholungsversuch.
- `UnexpectedReboot`: Exitcode 1641, selbst wenn er als zulässig eingetragen wurde. Installer, die selbst einen Neustart erzwingen, sind für specialize ungeeignet.
- `Blocked`: eine erforderliche Abhängigkeit ist fehlgeschlagen.

Abgeschlossene und fehlgeschlagene Einträge werden bei Wiederaufnahme übersprungen. Die erfolgreiche Erstbenutzerwarteschlange schreibt nur einen Abschlussmarker in einen separaten ProgramData-Ordner. Dieser Marker darf ausschließlich zur Entfernung der Startaufgaben dienen; er steuert keine privilegierten Befehle oder System-Installer. Nach erfolgreichem Systemabschluss und gegebenenfalls Benutzerabschluss entfernt die nächste Systemausführung beide Aufgaben. Protokolle, Status und Payload bleiben als nachvollziehbarer Nachweis erhalten.

## Projekte

Versionierte JSON-Dateien (`Version: 1`) speichern ISO, SHA-256, Installer, Parameter, IDs, Abhängigkeiten, Reihenfolge und Ausgabeoptionen. Projekte können auch vor vollständiger Konfiguration gespeichert werden. Vorhandene Dateien werden dabei gehasht; beim Öffnen und vor Erstellung werden gespeicherte Hashes geprüft. Fehlende oder veränderte Dateien werden gemeldet. Eine ISO über „Auswählen“ oder einen Installer über „Datei neu zuordnen“ aktualisieren; damit wird die vorherige Hash-Zuordnung ausdrücklich verworfen. EXE-Konfiguration danach erneut bestätigen. Relative Pfade werden relativ zum Projekt aufgelöst. Das Beispielprojekt liegt in [examples](examples/README.md).

## ISO-Verarbeitung und Grenzen

Windows bindet das Medium read-only ein. DISM liest boot.wim-Metadaten und prüft Windows-Version 10.x sowie x64. NovaForge kopiert sämtliche Dateien in einen eindeutigen Arbeitsordner, prüft WIM-/ESD-Header und bei SWM GUID, Teilnummern und Vollständigkeit. Alle Abbilder und Editionen bleiben erhalten; es erfolgt kein WIM-Mount, Export oder Recompression.

Vorhandene externe `autounattend.xml`, `unattend.xml`, `SetupComplete.cmd`, `ErrorHandler.cmd` oder `sources\$OEM$` führen zu einem Konflikt und Abbruch. Es gibt kein stilles Überschreiben und keine automatische Antwortdatei-Zusammenführung. Innerhalb der unveränderten WIM/ESD-Abbilder werden keine vorhandenen Herstelleranpassungen analysiert; deshalb originale Microsoft-Medien verwenden.

oscdimg erstellt UDF 1.02 und verwendet BIOS-`boot\etfsboot.com` und UEFI-`efi\microsoft\boot\efisys.bin`, soweit vorhanden. Danach wird die neue ISO erneut eingebunden: Alle Dateien werden anhand Größe und SHA-256 mit dem Arbeitsmedium verglichen, Antwortdatei und El-Torito-Bootkatalogsignatur/-Prüfsumme geprüft. Erst dann wird die temporäre Ausgabe auf den endgültigen Namen verschoben. Firmware-Bootfähigkeit ist damit technisch geprüft, aber ohne VM-Boot nicht praktisch abgenommen. [Microsoft: oscdimg-Optionen](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/oscdimg-command-line-options).

Arbeitsdateien und temporäre ISO werden bei Erfolg, Fehler und normalem Abbruch entfernt. Eigene Mounts werden freigegeben; bereits durch den Benutzer eingebundene Quell-ISOs bleiben eingebunden. Ein Prozess-/Stromausfall kann einen `NovaForge-<GUID>`-Arbeitsordner oder eine `.partial-<GUID>.iso` hinterlassen. Vor manuellem Löschen sicherstellen, dass kein Build mehr läuft und die Ausgabe nicht eingebunden ist.

## Fehlersuche

| Symptom | Maßnahme |
|---|---|
| oscdimg fehlt | ADK Deployment Tools installieren oder `oscdimg.exe` manuell auswählen. |
| Hash stimmt nicht | Unbeabsichtigte Änderung prüfen; bei beabsichtigtem Update Datei ausdrücklich neu zuordnen. |
| UAC abgebrochen / Zugriff verweigert | Erneut starten und nur den Mount-/Prüfschritt freigeben; lokales beschreibbares Arbeits-/Ausgabelaufwerk verwenden. |
| DISM liest Medium nicht | Passende aktuelle Windows-Hostversion und originales x64-Medium verwenden. |
| Setup hängt bei Programmeinrichtung | Systeminstaller darf keine Benutzeroberfläche oder Benutzeranmeldung verlangen. Zeitlimit und Silent-Argumente prüfen. |
| Warteschlange wartet | `state.json`, Abhängigkeiten und Neustartstatus prüfen. Bei 3010 Windows nach Setup manuell neu starten. |
| Benutzerprogramm verlangt Adminrechte | Systemkontext verwenden, sofern Hersteller dies unterstützt; Benutzerkontext wird bewusst nicht erhöht. |
| Fehler / Interrupted | Zielcomputer prüfen und Ursache beseitigen; kein blindes Wiederholen. |

Build-Protokoll: `<Ausgabe>.iso.log`. Systeminstallationen: `%WINDIR%\NovaForge\State\installation.log` und `state.json`; MSI zusätzlich `<ID>.msi.log`. Benutzerinstallationen: `%LOCALAPPDATA%\NovaForge`. Bootstrapfehler: `%WINDIR%\NovaForge\bootstrap-error.log`. Anwendungsfehler: `%LOCALAPPDATA%\NovaForge\application-error.log`. Windows Setup zusätzlich `%WINDIR%\Panther\UnattendGC\setupact.log`.

Eine gezielte Wiederholung ist eine administrative Wartungsaktion auf dem **Zielcomputer**: beide NovaForge-Aufgaben vorübergehend deaktivieren, den betreffenden Eintrag und gegebenenfalls davon abhängige `Blocked`-Einträge in der passenden `state.json` nach Prüfung entfernen, Aufgaben wieder aktivieren und eine neue Anmeldung beziehungsweise Systemaufgabe auslösen. Erfolgreiche Einträge nicht entfernen. Benutzerstatus mit diesem Benutzerkonto bearbeiten. Nach endgültigem Verzicht können die beiden geplanten Aufgaben manuell gelöscht werden. Keine Statusänderung an einer gerade laufenden Warteschlange vornehmen.

## Entwicklung und Veröffentlichung

.NET SDK 10.0.401 (global.json; neuere Patchversionen dieser SDK-Reihe sind zulässig) unter Windows installieren. Keine Drittanbieter-NuGet-Pakete erforderlich; Runtime-Pakete werden bei der ersten Veröffentlichung von NuGet geladen. Das lokal verwendete SDK liegt in `.tools\dotnet` und gehört nicht zum portablen Paket. `scripts\Package.ps1` erzeugt zusätzlich ein portables ZIP und ein Quellcode-ZIP mit Dokumentation, Beispiel und Testnachweisen. Die Runtime-Lizenzen werden bei Publish automatisch nach `dist\win-x64\licenses` übernommen.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Publish.ps1
# Optional ausdrücklich gewählter SDK-Pfad:
.\scripts\Publish.ps1 -Dotnet C:\SDK\dotnet.exe
```

Build führt Kern-, echte PowerShell-Prozess- und WPF-Layout-/Bindungsprüfungen aus; Bitmapexport wird bei verfügbarer Grafik zusätzlich gegen eine unabhängige Testfläche geprüft und andernfalls ausdrücklich als SKIP gemeldet. Tests installieren keine Produkte und registrieren keine geplanten Aufgaben auf dem Host. Der harmlose Testprozess wird aus `tests\Fixture.cs` mit dem vorhandenen Windows-.NET-Framework-Compiler erstellt. Die Tests laufen auch mit der für Zielsysteme relevanten Windows PowerShell 5.1.

Publish verwendet `win-x64`, `--self-contained true`, `PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`, Kompression, **kein Trimming** und keine externen PDBs. Ergebnis: eine startbare `NovaForge.exe`. Quellcode und Tests liegen in `src`, `scripts` und `tests`. Architektur: [ARCHITEKTUR.md](ARCHITEKTUR.md).
