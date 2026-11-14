# Changelog – Fersa Surfer

Format: [Keep a Changelog](https://keepachangelog.com/de/1.1.0/) · Versionen nach [SemVer](https://semver.org/lang/de/).

## [Unveröffentlicht]
### Hinzugefügt (Web-Vorschau)
- Power-ups (Surfboard, Magnet, Schild, 2×), Combo-Multiplikator, Partikel, Speed-Linien, Tag-Nacht-Zyklus, live erzeugte Musik + Soundeffekte.
- Grafik-Upgrade: Toon-Shading, Bloom-Leuchten, Figur mit großem Kopf, Augen und Comic-Konturen.
- Spielbare Browser-Version mit Welt-Editor: zeichnen → echte 3D-Häuser, Bäume, Seen, Gärten; Strecke bleibt automatisch frei.

### Hinzugefügt
- GitHub Actions: Kern-Tests bei jedem Push; Cloud-Build für Android, Windows und WebGL per Knopfdruck (GameCI).
- Welten speichern/laden (JSON, atomar, manipulationssicher) + AtomicFile; 11 neue Tests.
- M4/M5-Vorarbeit: Zeichen-Dokument mit Rückgängig/Wiederholen, Linienglättung, semantische Pinsel → sicherer Weltplan (Strecke geprüft, Objekte von der Strecke geschoben); 15 neue Tests inkl. Masterplan-Abnahmeszenario.
- M7-Vorarbeit: Outfit-Textparser (DE/EN) + Start-Katalog mit ehrlichem Ersatz-Hinweis; 14 neue Tests.
- `CiBuild.cs` (automatischer Build) und `docs/BUILD_IN_DER_CLOUD.md`.

## [0.1.0] – 2026-11-14 · M0 + M1 (vorbereiteter Unity-Projektstand)
### Hinzugefügt
- Projektstruktur (Unity 6 / URP), `Packages/manifest.json`, `.gitignore`, `.gitattributes`.
- Spiellogik ohne Unity: 3 Spuren, Springen, Rutschen, steigendes Tempo, 3 Hindernistypen, Münzen, Punkte, Bestwert, Pause, Game Over, Neustart.
- Endlos-Strecke mit Seed und Sicherheitsprüfung (keine unschaffbaren Stellen).
- Wischgesten, Tastatur, Maus und Gamepad.
- Lokaler Spielstand mit atomarem Speichern und Sicherung.
- Unity-Prototyp: Szene aus einfachen 3D-Formen, Kamera, HUD.
- Editor-Menü „Fersa Surfer“: Demo-Szene erstellen, sichere Build-Einstellungen.
- 55 Kern-Tests (bestanden mit `dotnet test`) und 1 PlayMode-Test (noch nicht ausgeführt).
- Doku: README, AGENTS, CLAUDE, PLAN, ARCHITECTURE, PROJECT_STATE, OPEN_ISSUES, SECURITY, ADR-001 bis ADR-003, Archiv mit Masterplan v1.0.
### Geändert
- Produktname: „WORLD RUSH“ → **„Fersa Surfer“**. Der interne Code-Name bleibt `WorldRush`.
