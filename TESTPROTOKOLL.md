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

# NovaForge 1.3 Beta · Prüfprotokoll

Geprüft werden der geführte Ablauf pro Software, Pflichtaufnahmen im Projekt, Entwurf → vollständige Aufnahme → frischer Test → ISO-Freigabe und die Entwertung bei geänderten Paketen. Die echte WPF-Oberfläche prüft Namen im Kopf und in der Titelleiste, Bindings, Seitenlayout und die gesperrte ISO-Erstellung für unvollständige Software. Screenshots werden nur verwendet, wenn eine unabhängige Renderprobe tatsächlich Pixel erzeugt.

Vorhandene Kern-, Runner-, Versions- und Automationsprüfungen sowie der portable Starttest bleiben Bestandteil des Release-Builds. Harmloser Fixture-Prozess statt echter Software-/Treiberinstallation.

Windows Sandbox ist auf diesem Gerät weiterhin deaktiviert. Der komplette Gast-/VNC-/Treiber-/ISO-Ablauf wurde nicht end-to-end ausgeführt. Die neue Sandbox-Oberfläche wird kompiliert; konkrete Gastdarstellung und Hardwaretreiber benötigen zusätzliche Testumgebung. Kein universelles „alle Installer funktionieren“ zugesichert.
