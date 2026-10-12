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
