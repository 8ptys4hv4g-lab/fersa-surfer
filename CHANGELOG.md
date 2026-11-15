# Changelog – Fesara – Run & Surf

Format: [Keep a Changelog](https://keepachangelog.com/de/1.1.0/) · Versionen nach [SemVer](https://semver.org/lang/de/).

## [0.4.3] – 2026-11-15
### Behoben
- Knopf-Texte werden nicht mehr abgeschnitten (z. B. „Nächstes Level“): alle Knöpfe verkleinern die Schrift automatisch, wenn nötig.
- Level-geschafft-Karte zeigt gezeichnete goldene Sterne statt „**_“.

## [0.4.2] – 2026-11-15
### Hinzugefügt
- **Gemalter Himmel** statt einfarbigem Lila: Verlauf vom Horizont über Abendrot zum dunkleren Zenit, weiche Wolkenstreifen, Sonne (auch im Startmenü sichtbar), nachts Mond und Sterne. Wechselt mit den 4 Himmelsfarben beim Laufen. Per Code gemalt (`Core/Visuals/SkyPainter.cs`), Neumalen in kleinen Stücken ohne Ruckeln.
- Build-Menüs tragen den Himmel-Shader automatisch in „Always Included Shaders“ ein (`ShaderIncludes.cs`). Fehlt er, bleibt der alte einfarbige Himmel.
- 5 Tests für den Himmel (Horizont = Nebelfarbe, Sonne in beide Richtungen, Mond nachts, Stück-für-Stück-Malen gleich).

## [0.4.1] – 2026-11-15
### Geändert
- **Neuer Name: „Fesara – Run & Surf“** (vorher „Fersa Wave“; Grund: „FERSA“ ist eine fremde Marke). Name unter dem Icon: „Fesara“, Untertitel englisch „Run & Surf“.
- **Neues Logo „Platsch-Welle“** als App-Icon und Store-Titelbild: per Code gezeichnet (`Core/Branding/IconPainter.cs`), eigenes Motiv. Unity-Menü „Fesara → 6.“ erzeugt die Dateien.
- Startmenü zeigt den Schriftzug „Fe“ (lila) + „sara“ (pink) und „RUN & SURF“.
- Paketname `com.fesara.game`, Firmenname „Fesara“, Unity-Menü heißt „Fesara“. Speicherdatei unverändert.
### Behoben
- Startmenü: Figur stand hinter der Menü-Karte (Kamera war spiegelverkehrt) – jetzt rechts daneben sichtbar.
- Punkteanzeige überdeckt im Menü nicht mehr die Karte; Versionsnummer unten rechts.
### Hinzugefügt
- 3 Tests für Name und Logo (Icon undurchsichtig, Farben an den richtigen Stellen, Store-Größen).

## [Unveröffentlicht – ältere Einträge]
### Geändert
- **Neuer Name: „Fersa Wave“** (vorher „Fersa Surfer“) – eigenständiger, Abstand zu bekannten Marken. Paketname `com.fersawave.game`, Menü in Unity heißt jetzt „Fersa Wave“. Spielstand bleibt erhalten (Speicherdatei unverändert).
### Geändert
- Schnelleres Spielgefühl: Start 13 m/s (vorher 10), Höchsttempo 27 m/s (vorher 24), Tempo steigt schneller (0,22 statt 0,15 m/s²). Alle 132 Tests inkl. Test-Bot-Durchläufe bestanden.
### Hinzugefügt
- Optionen: „Standard-Stadt wiederherstellen“ und „Alles zurücksetzen“ (jeweils mit Bestätigung); Hinweis im Startmenü, wenn eine eigene Welt aktiv ist.
### Hinzugefügt
- Welt-Editor im Unity-Spiel (Tab „Welt“): Häuser, Baumreihen und Gärten zeichnen → „In 3D bauen“ → eigene Welt wird gespeichert und beim nächsten Start wieder geladen; Beispiel-Welt; zurück zur Standard-Stadt.
### Verbessert
- Touch-Wischgesten direkt vom Finger (zuverlässiger auf Handys/Tablets).
- Hindernis-Ansichten werden wiederverwendet statt neu gebaut (weniger Ruckler auf schwachen Geräten).
- F12 im Spiel: Store-Screenshot in doppelter Auflösung (`StoreAssets/Screenshots`).

## [0.4.0] – 2026-11-15 · Store-Vorbereitung
### Hinzugefügt
- Deutsch/Englisch für alle Texte (automatisch nach Systemsprache, umstellbar).
- Optionen: Musik, Geräusche, Grafikstufe (Sparsam/Mittel/Hoch für schwache Handys), Sprache, Tutorial wiederholen, Credits & Lizenzen.
- Tutorial beim ersten Spielen (Hinweise zu Wand, Hürde, Balken, Salto).
- Pause bei App-Wechsel/Anruf, Bildschirm bleibt beim Spielen an.
- Editor: „6. Store-Einstellungen + App-Icon“ (eigenes Icon + Store-Grafiken, Paketname, Version, IL2CPP, ARM64, .aab, Querformat) und „7. Android-AAB bauen“ (mit Keystore-Prüfung).
- Store-Unterlagen: Datenschutzerklärung DE/EN, Store-Texte DE/EN, Antworten Altersfreigabe/Datensicherheit/Familien-Richtlinie, Release-Checkliste.
- Unity-Kompilierprüfung ohne Unity (`tools/UnityCompileCheck`).
### Geändert
- Nach einem Aufprall: Ergebnis 2 s zeigen, dann zurück ins Startmenü (kein automatischer Neustart). Start nur per Knopf oder Enter. Sieg-Bildschirm mit „Menü“-Knopf.
### Hinzugefügt
- Tägliche Aufgaben (3 pro Tag, Belohnung in Münzen), Münz-Konto, Surfbrett-Shop (6 Bretter, nur Spiel-Münzen, rein kosmetisch), Tabs Start/Aufgaben/Shop; Zähler für Sprünge, Rutschen, Power-ups (10 neue Tests, 125 gesamt).

## [0.3.0] – 2026-11-15
- Version für Level-System, Surf-Abschnitte, Material-Umwandlung beim Import.
### Hinzugefügt
- Neue Spielidee: Levels mit Ziellinie und bis zu 3 Sternen, Surf-Abschnitte auf Wasser mit Wellen, Salto-Trick in der Luft, Level-Auswahl, Fortschrittsbalken, Sieg-Bildschirm (Kern: 9 neue Tests, 114 gesamt).
### Hinzugefügt
- Echte Stadt-Modelle aus `Resources/Environment/{Buildings,Props,Cars}`: Häuserreihen, parkende Autos, Deko; Gehwege und Parkstreifen; Neon-Hochhäuser als Skyline.
### Hinzugefügt
- Figurenauswahl im Startmenü (< >), Figuren auch aus Unterordnern; Menü „3. 3D-Figuren neu importieren“.
### Hinzugefügt
- Echte 3D-Figuren: Modell in `Assets/_WorldRush/Resources/Characters` ablegen → wird automatisch skaliert, animiert (Laufen/Springen/Idle) und nach Outfit-Text eingefärbt. Ohne Modell bleibt die Platzhalter-Figur.

## [0.2.0] – 2026-11-14
### Bestätigt
- M0 + M1 laufen in Unity 6.6 unter Windows.
### Hinzugefügt
- Kern: Power-ups (Surfboard, Magnet, Schild, 2×), Combo-Multiplikator, Münzspur in der Luft, Schonzeit nach Landung/Schild; 10 neue Tests (105 gesamt).
- Unity: Neon-Garden-City-Look (leuchtende Fenster, Laternen, Bloom, Farbkorrektur, Vignette), Tag-Nacht-Himmel, niedliche Figur mit Outfit aus Text, Partikel, synthetische Sounds und Musik, neues HUD.
### Geändert
- Input-System-Paket entfernt (Version passte nicht zu Unity 6.6, Kompilierfehler). Steuerung läuft über den eingebauten Input Manager (Tastatur, Maus, Touch).

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
