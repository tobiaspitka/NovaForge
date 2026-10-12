# NovaForge 1.3.0-beta.6 · Robustere Erkennung und neu zuordnen

Die Zielsuche bewertet erreichbare Elemente ausschließlich in zugeordneten Installerprozessen. Stabile Kennung plus normalisierte Beschriftung erkennen ein Ziel auch bei geändertem Fenstertitel oder Klassenname. Bei geändertem Element-ID kann eine eindeutige Beschriftung mit passendem Fenstertitel und Klasse helfen. Beschleunigertasten (&), Auslassungspunkte, Groß-/Kleinschreibung, Leerzeichen und Versionssuffixe im Titel werden berücksichtigt. Nicht eindeutig wiederauffindbare Elemente werden auch aus den Korrekturvorschlägen ausgeschlossen. Andere Beschriftungen bei wiederverwendeter Kennung, falsche Prozesszuordnung, fehlende Aktionsmuster, verdeckte/deaktivierte oder mehrdeutige Elemente führen zu keiner automatischen Aktivierung.

Bei fehlendem oder mehrdeutigem Ziel bietet der Sandbox-Test „Installationsschritt neu zuordnen“. Der Nutzer wählt ein sichtbares kompatibles Element, kann noch einmal suchen oder den Test beenden. Die Aktion und Eingabewerte bleiben erhalten; nur die Zielkennung wird geändert. Gewöhnliche Texteingaben können dabei nicht auf Kennwortfelder umgebogen werden. Die neue Zuordnung wird als ungeprüfter Entwurf gesichert. Erst ein weiterer unveränderter Durchlauf in einer neuen Sandbox gibt sie für die ISO frei. ISO-Wiedergabe fragt nicht interaktiv nach einer Zuordnung.

Die Diagnose nennt zugängliche zugeordnete Fenster und mögliche Gründe wie noch nicht geöffnetes, verdecktes oder geändertes Ziel. Bestehende Aufnahmen bleiben kompatibel. Kein blindes Klicken gespeicherter Bildschirmkoordinaten und keine Garantie für alle benutzerdefinierten Installer oder geschützte UAC-Abfragen.

Neue echte Prozessprüfungen decken Titel-/Klassenänderung, geänderte IDs mit Versionssuffixen, widersprechende Beschriftungen, Mehrdeutigkeit, Prozessgrenzen und die Korrektur eines fehlenden Felds mit automatischer Fortsetzung ab. Reparatur erhält den Eingabewert, entwertet die alte Freigabe und verlangt erneut einen frischen Test. Sandbox-Dialog wird kompiliert; vollständige VNC-/Gast-/ISO-Installation hier weiterhin nicht end-to-end ausgeführt.

# NovaForge 1.3.0-beta.4 · Automatische Wiedergabe und verständliche Schritte

Die gesamte UI-Automation der Wiedergabe läuft außerhalb des WPF-Fensterablaufs auf MTA-Hintergrundthreads. Schaltflächen werden über einen eigenen Helfer ausgelöst, der das Ziel selbst erneut auflöst; ein laufender Invoke-Aufruf sperrt nicht die Suche nach dem nächsten Dialog. Vor erfolgreicher Freigabe müssen die Auslösungen abgeschlossen, der Installer erfolgreich beendet und die neue Prüfdatei vorhanden sein. Es werden keine gespeicherten Bildschirmkoordinaten blind geklickt.

Die Fenstersuche berücksichtigt auch passende Unterdialoge innerhalb eines zugeordneten Prozessfensters und weist mehrdeutige Ziele weiterhin zurück. Neue Aufnahmeschritte speichern die Prozessdatei-Prüfsumme auch für normale Installerprozesse, damit spätere getrennte Launcher-/Treiberfenster gezielt erkannt werden können.

Die Oberfläche zeigt „Schaltfläche betätigen“, „Eintrag auswählen“, „Häkchen setzen“ und „Text eingeben“ statt Invoke/Select. Die gespeicherten Aktionen bleiben kompatibel. Im Test wird der aktuelle Schritt markiert. Wartehinweise enthalten Schrittnummer, erwartetes Fenster und verbleibende Wartezeit. Das Wiedergabeprotokoll bleibt nach Bereinigung unter der Aufnahmebibliothek/Diagnostics erhalten; vertrauliche Werte werden nicht ausgegeben.

Neue echte Prozessprüfung: Radio-Auswahl und zusätzlicher modaler Bestätigungsdialog werden zuerst mit echten UIA-Selektoren erfasst und anschließend in einer neuen Fixture-Instanz automatisch wiedergegeben. Der Aufruf kommt dabei wie beim Recorder aus einem STA-Dispatcher; alle Automationsmeldungen müssen vom MTA-Worker kommen und der Dispatcher muss weiter reagieren.

Microsoft dokumentiert mögliche Blockaden durch Invoke und die Verwendung eines separaten Helfers: https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.invokepattern.invoke

Prüfgrenze: Diese Tests verwenden ein eigenes harmloses Programm. Vollständige Sandbox-/VNC-Treiber-/ISO-Installation hier weiterhin nicht ausgeführt. Nicht zugängliche UAC-Dialoge bleiben manuell; einzelne Installer können zusätzliche Anpassungen benötigen.

# NovaForge 1.3.0-beta.3 · Sitzungsbereinigung und Aufnahmen löschen

Eine gesperrte NovaForge.Beta.exe oder schreibgeschützte temporäre Kopie verursacht keinen falschen Übernahmefehler mehr. Die Bereinigung wartet bei Zugriffs-/Dateisperren und wird automatisch erneut versucht. Übernahmedaten bleiben erhalten, bis die großen Kopien gelöscht sind. Bestätigte Exporte werden auch nach einem Neustart nicht erneut importiert. Die Hostbestätigung wird vor der internen Kennzeichnung geschrieben, damit fehlgeschlagene Bestätigungen erneut versucht werden.

In „Meine gespeicherten Aufnahmen“ kann die ausgewählte Aufnahme nach einer Namensbestätigung gelöscht werden. Zuordnungen im geöffneten Projekt werden entfernt; die betroffenen Programme müssen neu zugeordnet oder aufgenommen werden. Andere Aufnahmen, exportierte Dateien und Versionssicherungen bleiben erhalten. Während einer Sandbox-Sitzung ist das Löschen gesperrt.

Regressionen prüfen eine echte Dateisperre der temporären EXE, unveränderte Übernahmedaten beim Aufschub, erfolgreiche Wiederholung mit schreibgeschützter Datei sowie Löschung genau einer Aufnahme und Schutz vor Pfaden außerhalb der Bibliothek. Der vollständige Sandbox-/VNC-/ISO-Ablauf ist hier weiterhin nicht ausgeführt.

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
