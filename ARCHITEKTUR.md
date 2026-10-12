# Architektur

NovaForge besteht aus drei klar getrennten Ausführungsebenen.

**WPF / MVVM (`src/NovaForge`)**: MainWindow stellt die fünf deutschen Arbeitsbereiche einschließlich Setup-Menü dar. MainViewModel hält Projektzustand und Befehle; Installer implementiert INotifyPropertyChanged für die editierbaren Einträge. Datei-/Ordnerauswahl und Shell-Öffnen erfolgen lokal. Langläufer laufen über Tasks außerhalb des UI-Threads; Fortschritt wird über IProgress zum Dispatcher übertragen. Während eines Builds sind Konfigurationsänderungen gesperrt; Fenster, Protokoll und Abbruch bleiben bedienbar.

**Kern (`src/NovaForge.Core`)**: Versioniertes Projektmodell und JSON-Persistenz, Hashberechnung, Exitcoderegeln, stabile topologische Sortierung und Validierung. IsoBuilder orchestriert Speicherprüfung, Mount/Kopie, Medien-/Konfliktprüfung, Payload-Integration, oscdimg, vollständigen Dateivergleich, Bootkatalogprüfung, Ausgabehash und Bereinigung. ArgumentList verhindert Shell-Injektion bei oscdimg; die ShellExecute/UAC-Grenze verwendet explizites Windows-Argumentquoting. Ausschließlich die ISO-Mount-Hilfsprozesse werden erhöht. PowerShell-Ressourcen sind in der Assembly und damit in der portablen EXE eingebettet.

**Zielsteuerung (`scripts`)**: autounattend.xml startet Bootstrap in specialize. Bootstrap setzt ACLs und registriert SYSTEM- und interaktive Benutzeraufgaben. Runner prüft Hashes, führt Installer nacheinander aus und speichert Zustand atomar. Ein Mutex verhindert doppelte gleichzeitige Ausführung im jeweiligen Kontext. Ein Bootzeitstempel bildet die Neustartbarriere ab. Der Runner ist mit Windows PowerShell 5.1 kompatibel; auf frisch installiertem Windows ist kein .NET-10-Runtime-Setup erforderlich.

SYSTEM-Status befindet sich im nur administrativ beschreibbaren Payload-Ordner, Benutzerstatus im eigenen AppData. Systemprogramme können von Systemprogrammen abhängen; Benutzerprogramme von beiden Kontexten. Umgekehrte Abhängigkeiten werden abgelehnt. Fehler bleiben terminal, bis gezielt administrativ korrigiert wird; erfolgreich abgeschlossene Programme werden nicht erneut installiert.

**Tests**: Ein paketfreier .NET-Testläufer prüft Kernlogik. Windows-PowerShell-Integration startet kleine echte Testprozesse mit definierten Exitcodes und Zeitverhalten in isolierten temporären Verzeichnissen. MSI-Aufruf wird mit einem eigenen Prozess als msiexec-Ersatz geprüft; eine reale MSI-Installation wird dadurch nicht behauptet. Ein WPF-Testhost prüft fünf echte Seiten und prüft das Erhalten benutzerdefinierter Parameter beim Wechsel der Auswahl. Firmware-Boot/Windows-Setup/ADK-Erstellung benötigen die noch ausstehende VM-Abnahme.

**Setup-Konfiguration**: SetupOptions trennt schaltbare Angaben von temporären Kennwörtern. UnattendBuilder erzeugt windowsPE-, specialize- und oobeSystem-Komponenten mit XML-Escaping. Projekte migrieren von Format 1 auf 2; Kennwörter sind von JSON ausgeschlossen.

**Versionskette**: Ensure-Version erkennt Änderungen eines bereits gesicherten Standes. Build schreibt einen Quellhash-Nachweis nach bestandenen Tests. Publish prüft die selbstenthaltende EXE und Store-Version erstellt einen unveränderten lokalen Versionsstand. GitHub Releases bieten getrennte Downloads; lokale Dateien und Zugangsdaten bleiben außerhalb von Git.

## Beta-Recorder
SandboxSessions trennt schreibgeschützte Eingaben und beschreibbare Ausgaben. RecorderWindow erfasst UIA-Steuerelemente. RecordedProfile bindet den Ablauf an Installer-Hash und Prüfdatei. Erfolgreiche Testwiedergabe bestätigt den unveränderten Plan. IsoBuilder übernimmt den portablen Replay-Host und validierte Profile; Runner prüft Profil-Hashes. Beta-Builds liegen getrennt unter dist/beta.
