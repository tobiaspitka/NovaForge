# NovaForge 1.3 Beta · Geführte Softwarevorbereitung

Jede aktive Software durchläuft einen verständlichen Ablauf: **Installation vormachen → Aufnahme abschließen → in einer neuen Sandbox testen → für ISO freigegeben**. Die ISO-Erstellung bleibt gesperrt, solange ein aktives Programm noch nicht vollständig geprüft ist. Vorhandene Projekte und Aufnahmen bleiben nutzbar; alte Silent-Konfigurationen müssen für diesen geführten Ablauf aufgenommen werden.

Unter Programme einen Installer hinzufügen und den Programmnamen prüfen. Bei EXE-Dateien wird nach Möglichkeit der Produktname gelesen; der Anzeigename kann frei geändert werden. Der Kopfbereich und die dunkle Titelleiste zeigen das aktuelle Programm. Treiber-/Begleitordner und technische Optionen sind aufklappbar.

Im Installationsstudio zeigt die zentrale Schaltfläche den nächsten erforderlichen Schritt. Windows Sandbox kann dort vorbereitet werden. Die erste Sitzung installiert und zeichnet auf. Aufnahme mit installiertem Prüfnachweis speichern und schließen. Nach Hostübernahme und geschlossener Sandbox folgt der Test in einer neuen Sitzung. NovaForge verhindert den Start einer zweiten Aufnahme-/Testsitzung, solange die vorherige Sandbox geöffnet ist.

Nach erfolgreichem Test ist das Programm als „Für ISO geprüft“ markiert. „Nächstes Programm“ führt zur nächsten unvollständigen Software. Eine Änderung des Installerpfads, Hashes, Begleitordners oder Ablaufs hebt die Freigabe auf. Reine Anzeigenamen ändern die geprüfte Ausführung nicht. Gespeicherte Aufnahmen bleiben in der Bibliothek verfügbar.

Auch das Sandbox-Fenster hat jetzt einen Programmkopf, verständliche Schritte, einen eigenen Aufnahmenamen, aufklappbare Hilfe und getrennte Aktionen für Entwurf und Abschluss. Die Aufnahme bleibt ein Entwurf, solange Schritte oder Prüfdatei fehlen. Ein erfolgreicher unveränderter Test ist für die ISO verpflichtend.

Windows Sandbox und die vollständige VNC-/Treiber-/ISO-Installation wurden hier nicht ausgeführt. UI, Statusübergänge, Freigabesperre und vorhandene Automations-/Paketfunktionen werden geprüft. UAC bleibt manuell; hardwareabhängige Treiber können in Sandbox scheitern. Vertrauliche Werte bleiben im Profil und auf der ISO im Klartext.
# NovaForge 1.2.0-beta.6 · Aufnahme- und Speicherfix

- Aufnahme erfasst UIA-Ereignisse und beobachtet Klicks über vorab gelesene Selektoren. Keine UIA-/Dateiprüfung im zeitkritischen Eingabe-Hook. Aufnahmefehler werden angezeigt und protokolliert.
- Installer wird im Gast automatisch gestartet; zusätzliche Installations-/Treiberfenster werden standardmäßig mit erfasst. Über den Schalter kann dies abgeschaltet werden. UAC auf dem geschützten Desktop bleibt manuell.
- Aufnahme jederzeit als Entwurf speichern, auch bei laufendem Installer oder fehlender Prüfdatei. Entwürfe bleiben in der Bibliothek, benötigen vor ISO-Nutzung aber einen erfolgreichen frischen Test.
- Namen im Host und Gast frei vergeben. Gespeicherte Aufnahmen über „Name / Prüfdatei bearbeiten“ ändern und mit selbst gewähltem Dateinamen exportieren.
- „Speichern und schließen“ wartet auf die Bestätigung des Hosts. Derselbe Sitzungsordner kann mehrere aktualisierte Profilstände übertragen; veraltete Bestätigungen werden nicht akzeptiert.
- Begleitordner mit Installer, Treiberdateien und Unterordnern auswählen. Originaldateiname und Verzeichnisstruktur bleiben in Sandbox und ISO erhalten. Paketinhalte sind prüfsummengebunden. Im Gast und bei Wiedergabe wird eine beschreibbare Arbeitskopie verwendet.
- Neu installierte Dateien unter Windows/System32/drivers sind als Prüfnachweis möglich. Ordnerpfade innerhalb eines Installerpakets werden für die Wiedergabe angepasst.

In der Sandbox F7 für eine fokussierte Aktion verwenden, falls ein Anbieter kein UI-Ereignis liefert. F8 übernimmt Text, F9 vertrauliche Werte. Vertrauliche Profile und ISO-Medien enthalten diese Werte im Klartext.

Treiber, die echte Hardware, einen Kernelneustart oder nicht verfügbare Geräte benötigen, können in Windows Sandbox scheitern. Für diese Fälle ist eine passende Test-VM bzw. Testhardware erforderlich. Die Beta umgeht diese Windows-Sandbox-Grenzen nicht. Die vollständige Sandbox-/ISO-Abnahme wurde auf diesem Gerät nicht durchgeführt; genaue Prüfgrenzen stehen im Testprotokoll.
## 1.2.0-beta.3

- Automatische Aufnahmeübernahme, persistente Bibliothek und direkte Zuordnung zu passenden Installer-Versionen.
- Aufnahme und Wiedergabe mit Administratorrechten; explizit verbundene Zusatzfenster mit Prozessprüfsumme.
- Gezielte Lizenz-/Kennwortschritte über F9 mit deutlicher Vertraulichkeitskennzeichnung.
- Windows-Sandbox-Aktivierungsbutton ohne automatischen Host-Neustart.
- Große Sandbox-Sitzungskopien nach sicherer Übernahme und geschlossenem Gast bereinigen; kleine Diagnosen erhalten.
- USB-/Medienordner als schreibgeschützte Quelle und Rückwandlung zur ISO ohne zusätzliche Anpassungen.
# Beta 1.2

- Neue Seitennavigation und sichtbar gekennzeichnetes Beta-Design.
- Installationsstudio mit Sandbox-Aufnahme und frischer Testwiedergabe.
- Semantische UI-Automation-Profile mit Installer-Prüfsumme und Erfolgskriterium.
- Wiedergabe auf dem Zielsystem; getrennte Beta-Ausgaben, Releases und lokale Versionsstände.
# Änderungen

## 1.1.1

- Kennwortfelder werden beim Wechsel des Projekts geleert, passend zum nicht gespeicherten Kennwort.
- Deterministische Versionsmetadaten verhindern, dass ein reiner Git-Commit eine andere EXE mit gleicher Versionsnummer erzeugt.
- Versionsverwaltung prüft unveränderte und veränderte Quellstände sowie das Zurückweisen ungetesteter Sicherungen in isolierten Tests.

## 1.1.0

- Neues Menü „Setup-Optionen“ mit einzeln schaltbaren Anpassungen.
- Lokales Benutzerkonto mit Kennwort und wählbarer Administratorrolle über `oobeSystem/LocalAccounts`. Kennwörter werden nicht in Projekten oder Protokollen gespeichert.
- Getrennte experimentelle TPM-, Secure-Boot- und RAM-Setupprüfungen im `windowsPE`-Durchlauf.
- Buildabhängiger Offline-OOBE-Schalter für „Ich habe kein Internet“ mittels BypassNRO.
- Computername, Region/Tastatur, Zeitzone, OEM-Registrierung und Langpfade.
- Projektformat 2 mit Migration bestehender Version-1-Projekte. Setup-Anpassungen sind auch ohne Programme möglich.
- Versionierte lokale Sicherungen einschließlich Quellcode, EXE, SHA-256 und Testnachweisen nach erfolgreicher Veröffentlichung. Geänderter Quellstand erhält automatisch die nächste Patchversion, wenn die aktuelle Version bereits gesichert ist.
- GitHub Actions für Windows-Builds und GitHub Releases mit getrennten EXE-Downloads pro Version.

Die Windows-Setup-/VM-Abnahme steht aus. LabConfig und Offline-OOBE sind buildabhängig und werden nicht als allgemein garantiert angeboten.

## 1.0.0

- WPF-Oberfläche für Windows-ISO, lokale MSI-/EXE-Installer, Abhängigkeiten, Projekte und Ergebnis.
- OEM-Payload, specialize-Installationssteuerung und wiederaufnehmbare Warteschlange.
- Bootfähige ISO-Erstellung mittels Microsoft oscdimg; Dateihash- und Bootkatalogprüfung.
- Portable, selbstenthaltende Windows-x64-EXE mit .NET 10 LTS.

## Nächste Beta · Anzeigename der zugeordneten Aufnahme
Unter Programme erscheint der vergebene Aufnahmename. Der technische Dateipfad ist separat aufklappbar. Namen werden beim Zuordnen, Umbenennen und erneuten Projektöffnen geladen.
