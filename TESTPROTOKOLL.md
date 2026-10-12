# Testprotokoll · NovaForge 1.0

Stand: **12. Oktober 2026**, Windows 11 Pro x64, Build **10.0.26300**. Verwendet: .NET SDK **10.0.401**, .NET/WPF-Runtime **10.0.12**, Windows PowerShell **5.1**. Keine ursprüngliche Projektbasis vorhanden; Anwendung neu erstellt.

## Tatsächlich durchgeführt

| Prüfung | Ergebnis / Umfang |
|---|---|
| Release-Build | Erfolgreich, **0 Fehler, 0 Warnungen**. |
| Kern-Testläufer | **18 Prüfungen bestanden**. Sortierung, System-/Benutzerphasen, Zyklen, deaktivierte/fehlende Abhängigkeiten, doppelte Dateien, ungeprüfte EXE-Konfiguration, fehlende EXE-Silent-Parameter, Zeitlimit, Exitcodes, Wiederaufnahmeregeln, Windows-Argumentquoting, specialize-Antwortdatei, eingebettete Skripte, SWM-Header/Vollständigkeit und Konflikte, JSON-Roundtrip, bekannte SHA-256-Testfolge. |
| Windows-PowerShell-Prozessintegration | **15 Prüfungen bestanden**. Echte, selbst kompilierte harmlose Prozesse mit Exitcode 0, 3010 und 5; Pause bis Bootmarkeränderung; keine Wiederholung; Abhängigkeiten blockieren; Zeitlimit beendet echten Prozessbaum; veränderte Datei wird nicht ausgeführt; Running wird Interrupted; Benutzerzustand getrennt; Systemabhängigkeit wird während derselben Anmeldung abgewartet; Abschlussmarker; echte MSI-Argumentübergabe an eigenen msiexec-Ersatz. |
| Leerzeichen / Umlaute | Integrationstests liefen in einem Arbeitsordner mit **„Ä mit Leerzeichen“**. Windows-Quoting mit Unicode, Leerzeichen, eingebetteten Anführungszeichen und abschließendem Backslash separat geprüft. |
| WPF-Renderprüfung | Alle **vier echten Anwendungsseiten** geladen und layoutet; keine Dispatcher-Ausnahmen. Wechsel der Programmauswahl erhält benutzerdefinierte EXE-Parameter. Frühe Renderdurchläufe waren erfolgreich und wurden visuell geprüft; dunkler Hintergrund, lesbare Dropdowns und Zeilenumbruch wurden dabei korrigiert. Ein zwischenzeitlicher Durchlauf lieferte in dieser Desktop-Sitzung leere Bitmaps; der Test erkennt diesen Fall anhand einer unabhängigen roten Testfläche und meldet dann ausdrücklich SKIP. Beim abschließenden NovaForge-Build war der Bitmapexport wieder verfügbar: alle vier Seiten wurden erfolgreich mit Farbprüfung exportiert. Nachweis: `artifacts/ui/render-status.txt` und `page-0.png` bis `page-3.png`. |
| PowerShell-Syntax | Skripte mit dem PowerShell-Parser geprüft; keine Syntaxfehler. Runner zusätzlich tatsächlich unter Windows PowerShell 5.1 ausgeführt. UTF-8-BOM für Skriptdateien und explizites UTF-8-Lesen für JSON. |
| Portable Veröffentlichung | Tatsächliche **win-x64-Single-File-EXE**, selbstenthaltend, native WPF-Komponenten eingeschlossen, kein Trimming. Das Ausgabeverzeichnis benötigt keine DLLs neben der EXE. Lizenztexte liegen separat bei. |
| EXE-Start | Endgültige EXE isoliert kopiert, echte WPF-Oberfläche geöffnet und normal mit Exitcode 0 geschlossen. `DOTNET_ROOT` und `DOTNET_ROOT_X64` zeigten dabei auf einen **nicht existierenden** Runtime-Ordner. Keine separat systemweit installierte .NET-10-Runtime vorausgesetzt. |
| Beispiel | Eigenen quelloffenen Beispielinstaller kompiliert und SHA-256 im JSON hinterlegt. **Nicht auf dem Erstellungscomputer ausgeführt.** |

Die 15 Integrationstests installieren keine echten Produkte und registrieren keine Aufgaben auf dem Host. Sie isolieren Zustand und Testprozesse in einem temporären Verzeichnis. Die Neustartfortsetzung wird über eine veränderte gespeicherte Bootkennung getestet, nicht durch Neustart des Arbeitscomputers. Der MSI-Ersatz prüft echte Prozessargumente; dies ist ausdrücklich **kein Nachweis einer realen MSI-Installation**.

Ausführbare Nachweise: `artifacts/final-build.txt`, `tests/runner-results.txt`, `artifacts/portable-start.txt`, `artifacts/ui/render-status.txt` und die vier Seiten-PNGs. Reproduzieren: `scripts/Build.ps1` beziehungsweise `scripts/Publish.ps1`.

## Nicht durchgeführt / verbleibende Abnahme

Hier sind keine Original-Windows-ISO, kein oscdimg und keine eingerichtete Windows-Test-VM bereitgestellt. Daher wurden **keine vollständige ISO mit ADK erzeugt, kein BIOS-/UEFI-Boot und keine Windows-Neuinstallation mit tatsächlichen Produktinstallationen durchgeführt**. Auch die Registrierung/Ausführung der geplanten Aufgaben während echter specialize/OOBE und deren Bereinigung sind in einer VM noch zu verifizieren. DISM-Medienprüfung und der Mount-/Ausgabeprüfpfad benötigen ebenfalls ein echtes Medium. Die SWM-Prüfung nutzt kontrollierte Headerfixtures, keine vollständigen Windows-Abbilder.

Nicht separat abgenommen: Windows-10-Host, abweichende DPI-Skalierungen/Monitore, sehr große Installerwarteschlangen, gleichzeitige Mehrbenutzeranmeldungen, OEM-Key-Installation, ARM-/S-/Audit-Modus (letztere sind außerhalb des unterstützten Ablaufs). EXE-Silent-Profile sind Vorschläge; Herstellerparameter müssen pro Installer geprüft werden. Die EXE ist nicht signiert.

## Konkreter VM-Abnahmeplan

1. Aktuelle ADK Deployment Tools installieren; echte Windows-10/11-x64-ISO verwenden. NovaForge auf dem Windows-Host starten, ISO zuordnen und quelloffenes Beispiel plus ein tatsächlich freigegebenes Offline-MSI und eine Offline-EXE hinzufügen.
2. Ein Systemprogramm und ein Benutzerprogramm mit unterstützten Silent-Parametern konfigurieren. Systemabhängigkeit vor Benutzerprogramm setzen. Projekt speichern und erneut öffnen; Hash-/Dateizuordnung prüfen.
3. ISO unter einem Pfad mit Leerzeichen und Umlauten erstellen. Beide Mount-Freigaben erteilen. oscdimg-Ende, komplette Dateihashes, Bootkatalog, Ausgabegröße und `.sha256` prüfen. Originalhash vor/nach Erstellung vergleichen.
4. Leere **UEFI-VM** mit Secure Boot und regulären Windows-Anforderungen booten. Windows normal installieren. Erhaltene Editionen prüfen. Danach bei Windows 10 separat **BIOS-VM** testen, soweit Quelle BIOS-Bootdaten enthält.
5. Installierte reale Produkte, Versions-/Uninstall-Einträge und `%ProgramData%\NovaForge-Beispiel\Installationsnachweis.txt` prüfen. System-/Benutzerprotokolle sowie Status mit tatsächlichen Installationen abgleichen. Programme dürfen nicht nur im Manifest stehen.
6. Neustartfall (3010) kontrolliert auslösen: kein Neustart während Setup, Pause sichtbar im Protokoll, manuell nach Setup neu starten, Fortsetzung ohne erneute Ausführung erfolgreicher Programme prüfen. Benutzerwarteschlange muss auf die vollständige Systemwarteschlange warten.
7. Fehler-/Timeout-Fälle und abhängige Programme prüfen. Bei erfolgreichem Abschluss müssen `NovaForge-System` und `NovaForge-User` verschwinden; Protokolle bleiben. OEM-Produktschlüsselfall separat testen, damit die gewählte specialize-Integration praktisch bestätigt wird.
8. Vorhandene externe Antwortdatei/OEM-Skripte, unvollständiges SWM-Set, fehlenden Platz, Abbruch während Kopie/oscdimg und verweigerte UAC-Freigabe gezielt prüfen. Keine Quelle oder bestehende Ausgabe darf verändert werden; eigene Mounts und Arbeitsdateien müssen freigegeben werden.

Erst nach diesen Prüfungen kann der vollständige Windows-Setup-Ablauf als praktisch abgenommen bezeichnet werden.
