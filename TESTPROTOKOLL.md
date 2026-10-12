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
