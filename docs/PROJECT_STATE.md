# Projektstand – Fersa Surfer

**Stand:** 2026-11-14 · Version 0.2.0 · **M0 + M1 ✅ in Unity bestätigt** · M2/M7 in Arbeit

## Pause – Stand 14.11.2026 (abends)
**Wo wir stehen:**
- Unity 6.6 läuft auf dem Windows-PC des Auftraggebers; Projekt unter `D:\Projekte\fersa-surfer`, mit GitHub verbunden (Update: `git -C D:\Projekte\fersa-surfer fetch; git -C D:\Projekte\fersa-surfer reset --hard origin/main`).
- Echte 3D-Figur (Quaternius „Ultimate Modular Women“, CC0) läuft mit Animation; Figurenauswahl im Startmenü (< >).
- Stadt-Modelle werden aus `Resources/Environment` geladen. Mit **Cartoon City Free** (Asset Store) erscheinen sie noch **pink** (Material-/Shader-Inkompatibilität); Ursache nicht abschließend geklärt, weil kein Console-Log vorlag. Diagnose-Logs (`Material umgewandelt: … (Shader '…')`) und Versionslabel `v0.2.0-mat3` sind eingebaut.
- HAON SD (Unity-chan/Misaki/Yuko) wird **nicht** verwendet: fremde Marken-Figuren mit eigener Lizenz.
- Fremde Pakete sind per `.gitignore` von GitHub ausgeschlossen (Lizenzschutz).

**Nächste Schritte beim Wiedereinstieg:**
1. Console-Screenshot nach ▶ holen → Shader-Namen der pinken Materialien → gezielt umwandeln, oder Unitys Render Pipeline Converter (Built-in → URP, Material Upgrade) ausführen.
2. Alternativ zur Quaternius-Stadt zurück (lief ohne Pink).
3. ✅ Neue Spielidee umgesetzt (Kern getestet, Unity-Teil ungeprüft): Levels mit Ziel + 3 Sternen, Surf-Abschnitte, Salto.
4. Ehrliche Einordnung: Ein „moderner“ Look braucht professionelle Grafik (Asset-Paket ca. 50–150 € oder 3D-Künstler).

## Update 0.2.0 (14.11.2026)
- ✅ **M0 + M1 bestätigt:** Projekt öffnet in Unity 6.6 (6000.6.9f1) unter Windows ohne Fehler; Spiel läuft im Play-Modus (vom Auftraggeber geprüft).
- Input-System-Paket entfernt (inkompatibel mit 6.6) → Steuerung über eingebauten Input Manager.
- Kern: Power-ups (Surfboard, Magnet, Schild, 2×), Combo-Multiplikator, Münzspur in der Luft – **105 von 105 Tests bestanden** (`dotnet test`).
- Unity (M2-Vorstufe, **noch nicht in Unity geprüft**): Neon-Stadt mit leuchtenden Fenstern, Laternen, Bäume, Bloom/Farbkorrektur/Vignette (URP-Volume), prozeduraler Himmel mit Tag-Nacht-Wechsel, niedliche Figur (großer Kopf, Augen, Zopf), Outfit aus Text im Startmenü, Partikel, synthetische Sounds + Musik, HUD mit Combo und Power-up-Balken.
- Web-Vorschau (three.js) mit Welt-Editor, Power-ups, Musik: `web-preview/index.html`.

## Stand 0.1.0
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
