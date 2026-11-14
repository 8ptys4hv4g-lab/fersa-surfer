# Projektstand – Fersa Surfer

**Stand:** 2026-11-14 · Version 0.1.0 · **M0 + M1: vorbereiteter Unity-Projektstand**

```text
FERSA SURFER — STATUS
Stand: M0 + M1 (vorbereitet, Spiellogik getestet)

Fertig umgesetzt:
- Spiellogik: 3 Spuren, automatisch laufen, Tempo steigt, Springen, Rutschen,
  3 Hindernistypen (Hürde/Balken/Wand), Münzen, Punkte, Bestwert,
  Pause, Game Over, Neustart
- Endlos-Strecke mit Seed + Sicherheitsprüfung (keine unfairen Stellen)
- Steuerung: Wischen (Touch + Maus), Tastatur, Gamepad
- Spielstand lokal, sicher gespeichert (mit Backup)
- Unity-Szene aus einfachen 3D-Formen + Kamera + Anzeige (Prototyp)
- Menü "Fersa Surfer" in Unity: Demo-Szene erstellen, sichere Build-Einstellungen
- Doku: README, AGENTS, CLAUDE, PLAN, ARCHITECTURE, SECURITY, CHANGELOG, ADRs, Archiv

Tatsächlich ausgeführt/getestet:
- `dotnet build` des Kerns als .NET Standard 2.1 / C# 9 (= Unity-6-Niveau): 0 Fehler, 0 Warnungen
- `dotnet test` (tools/CoreTests): 95 von 95 Tests bestanden (inkl. Welt-Editor-Abnahmeszenario, Welt speichern/laden) (inkl. Outfit-Text: "Blonde Haare, rotes Kleid, braune Schuhe."), u. a.
  - Strecke für 5 Seeds über 5 km sicher
  - Test-Bot läuft 5 Minuten (> 4 km) ohne Unfall, Speicher bleibt begrenzt
  - Keine "Tunnel"-Fehler bei Rucklern
  - Kaputte Spielstände werden sicher behandelt

Nicht ausgeführt / Grund:
- Unity-Kompilierung der Runtime-/Editor-Skripte: kein Unity in der Cloud-Umgebung
- PlayMode-Test RunnerSmokeTests: braucht Unity
- Paketversionen in Packages/manifest.json: nicht von Unity aufgelöst
- Android-/iOS-/PC-Build: braucht Unity + Gerät
- Optik/Performance (FPS): nicht gemessen

So kann ich es selbst ausprobieren:
1. Unity Hub → Unity 6 LTS installieren
2. Add project from disk → diesen Ordner
3. Menü "Fersa Surfer → 1. Demo-Szene erstellen"
4. ▶ Play drücken, mit Pfeiltasten oder Maus-Wischen spielen

Offene Fehler oder Kosten:
- Keine Kosten verursacht
- Siehe docs/OPEN_ISSUES.md

Nächster konkreter Schritt:
- Projekt in Unity öffnen, Fehlermeldungen aus der Console an Claude geben
- Dann M2 (schöne 3D-Stadt) + M3 (Menüs mit eigener Schrift)
```

## Testbefehl

```bash
cd tools/CoreTests && dotnet test
```
