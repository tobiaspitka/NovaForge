# Beta 1.2.0-beta.3 · Prüfstand

- 36 Kernprüfungen: unter anderem USB-/Ordnerkopie mit Bytevergleich, Quelländerungen, Manifest-Hashes, Pfadüberlappung, SWM-Sätze und Projektpersistierung.
- 15 echte Prozess-/Runnerprüfungen und 4 Versionsprüfungen.
- Sechs echte WPF-Seiten auf Layout, Bindings und Dispatcherfehler geprüft.
- 31 Automationsprüfungen mit der eigenen harmlosen Testanwendung: reale UIA-Aktionen, ausdrückliche Kennworteingabe, Geheimwerte aus normalen Logs ausgeblendet, separate Prozessfenster, Hashbindung, Bibliotheksübernahme und Zuordnung, Sandbox-Feature-Status, Bereinigungsgrenzen.
- Publish prüft die portable EXE ohne benachbarte DLLs und mit absichtlich ungültigem DOTNET_ROOT. Nur danach erfolgt die lokale Versionssicherung.

Windows Sandbox ist auf dem Entwicklungsgerät weiterhin deaktiviert. Der Aktivierungsbutton wurde nicht auf dem Host betätigt; Gastaufzeichnung, Gast-Shutdown, vollständige Sandbox-Sitzungsübernahme und echte Treiberinstaller wurden nicht durchlaufen. Die zugrunde liegenden UIA-/Bibliotheks-/Kopier-/Bereinigungsfunktionen wurden mit Testdaten geprüft. Für vollständiges USB→ISO/Windows-Setup fehlen echtes Testmedium und oscdimg; keine End-to-End-Abnahme behauptet.
