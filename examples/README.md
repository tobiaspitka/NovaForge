# Beispielprojekt

`Beispiel.novaforge.json` enthält einen eigenen, quelloffenen Offline-Beispielinstaller. Er schreibt bei `/silent` ausschließlich `%ProgramData%\NovaForge-Beispiel\Installationsnachweis.txt` und endet mit Exitcode 0. Er benötigt weder Internet noch proprietäre Programme und wird während der ISO-Erstellung nicht ausgeführt.

Die EXE wurde mit dem Windows-.NET-Framework-C#-Compiler aus `BeispielInstaller.cs` gebaut. Die Datei dient als leicht überprüfbarer Integrationsnachweis, nicht als Ersatz für den VM-Test mit realen MSI-/EXE-Produkten.

Projekt in NovaForge öffnen, Original-ISO auswählen, gegebenenfalls oscdimg zuordnen, Ausgabe wählen und erstellen. Der Platzhalter `Windows-original.iso` ist absichtlich nicht mitgeliefert. Relative Projektpfade werden relativ zur JSON-Datei aufgelöst. Das Programm nach einer Installation anhand der Nachweisdatei und `C:\Windows\NovaForge\State\installation.log` prüfen.

Beispiel neu bauen (Windows PowerShell):

```powershell
& "$env:windir\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:BeispielInstaller.exe BeispielInstaller.cs
```

Nach einem Neubau den Installer in NovaForge erneut zuordnen, damit die neue SHA-256-Prüfsumme übernommen wird. Zum Entfernen des Beispiels genügt das Löschen seines Ordners in ProgramData.
