# Beta 1.2.0-beta.6 · Prüfstand

Zusätzlich zu bisherigen Kern-/Runner-/Versionsprüfungen werden echte Aufnahmen getestet: UIA-Checkboxänderung, Abschlussaktion, Profilablage, beliebige Aufnahmenamen, Entwürfe ohne Prüfdatei, eindeutige Hostbestätigung und deren Entwertung nach Änderungen. Der native Klickpfad wird nur betätigt, wenn das eigene Testfenster tatsächlich im Vordergrund liegt; andernfalls wird der semantische UIA-Pfad geprüft und der native Teil ausdrücklich übersprungen. Die Testanwendung wartet kurz vor dem Prozessende, damit Providerereignisse zugestellt werden können.

Installerpakete werden mit harmlosen Dateien inklusive INF-/SYS-Begleitdateien auf Dateinamen, Struktur und Bytegleichheit geprüft. Die Dateien sind reine Testdaten; es wird kein Treiber installiert. Die sechs WPF-Seiten und die portable EXE werden weiterhin geprüft.

Windows Sandbox bleibt auf dem Entwicklungsgerät deaktiviert. Gastaufzeichnung, Gast-Shutdown und vollständige Aufnahmeübernahme in einer echten Sandbox wurden nicht durchlaufen. Echte Hardwaretreiber und vollständiges ISO-/Windows-Setup wurden mangels Testumgebung nicht geprüft. Lokale Versionsstände werden erst nach bestandenem Build und Starttest gespeichert.

Die Abfrage zusätzlicher Prozesse verwendet PROCESS_QUERY_LIMITED_INFORMATION. VNC mit virtuellem Bildschirmtreiber wurde hier nicht installiert. Aktueller lokaler Prüfstand: 38 Kern-, 17 Runner-, 4 Versions- und 40 Automationsprüfungen; sechs WPF-Seiten. Der native Klicktest wurde im letzten Lauf mangels Vordergrundfenster übersprungen; UIA-Ereignisaufnahme wurde real geprüft.

Registrierung der Aufnahmeereignisse erfolgt asynchron auf einem MTA-Worker; zusätzlich mit der echten WPF-Oberfläche auf weiterhin reagierende Dispatcher-Schleife geprüft. Ein separates Bestätigungsfenster wird durch den Recorder automatisch erfasst und an die Prozessdatei gebunden.
