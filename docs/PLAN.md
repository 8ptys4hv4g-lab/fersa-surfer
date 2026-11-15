# Plan – Fersa Wave

Die Grundlage ist der Masterplan v1.0 (`archive/2026-11-14_MASTERPLAN_v1.0.md`). Dieser Plan ist die **aktuelle, gepflegte** Fassung.

**Status-Zeichen:**
- ✅ fertig und geprüft
- 🟡 vorbereitet, aber nicht in Unity geprüft
- ⬜ offen

| # | Meilenstein | Status | Fertig, wenn … | Empfehlung Modell / Denkstufe |
|---|---|---|---|---|
| M0 | Setup: Repo, Unity-Struktur, Doku, Tests | ✅ | Projekt öffnet in Unity 6 ohne Fehler | Sonnet / Low |
| M1 | Spielbarer Kern: 3 Spuren, Sprung, Rutschen, Hindernisse, Münzen, Punkte, Game Over | ✅ in Unity bestätigt | Im Unity-Play-Modus spielbar | Opus / Medium |
| M2 | Premium-3D-Stadt „Neon Garden City“, Kamera, Figur mit Animationen | 🟡 Neon-Look + Platzhalter-Figur geschrieben, ungeprüft; Profi-3D-Figur fehlt (Kostenfreigabe) | Schöne 3D-Szene, FPS-Messung | Opus / High |
| M3 | Menüs mit eigener Schrift, Einstellungen, Highscore | ⬜ | Kompletter Offline-Spielablauf | Sonnet / Medium |
| M4 | Welt-Editor 2D: zeichnen, Pinsel, Rückgängig/Wiederholen, speichern | 🟡 Logik ✅ (getestet), Zeichen-UI + Speichern fehlt | Editierbarer Weltplan | Opus / High |
| M5 | Zeichnung → echte 3D-Welt | 🟡 Weltplan-Logik ✅, 3D-Aufbau fehlt | Straße, Häuser, Bäume und See werden 3D | Opus / High |
| M6 | Outfit-Studio: Avatar, Katalog, Farben | ⬜ | Outfits wählen und im Lauf tragen | Sonnet / Medium |
| M7 | Text-Outfits (DE/EN) | 🟡 Parser + Katalog-Logik ✅ (getestet), 3D/UI fehlt | „Blonde Haare, rotes Kleid, braune Schuhe.“ funktioniert | Sonnet / Medium |
| M8 | Android-/Tablet-Qualität, echte Testbuilds | ⬜ | Stabil auf Referenzgeräten | Opus / Medium |
| M9 | iOS, macOS, Linux, ggf. Web | ⬜ | Pro Plattform gebaut und getestet | Opus / Medium |
| M10 | Shop (nur Kosmetik), Analytics, Rechts-Check | ⬜ | Keine versehentlichen Käufe, faire Regeln | Opus / High |
| M11 | KI und Community (optional) | ⬜ | Kosten, Moderation und Sicherheit nachweisbar | Opus / High |

## Nächste Schritte
1. **Du:** Das Projekt einmal in Unity 6 öffnen, „Fersa Wave → 1. Demo-Szene erstellen“ wählen und ▶ drücken. Fehlermeldungen aus der Console an Claude schicken.
2. **Claude:** Gemeldete Fehler beheben. Danach ist M0/M1 ✅.
3. **Claude:** M3-Schrift vorbereiten: freie Schriftart mit OFL-Lizenz, Vorschlag „Fredoka“ oder „Baloo 2“, als Projektdatei einbinden.
4. **Claude:** M2 beginnen. Dafür nötig sind eigene 3D-Modelle und eine Figur. Asset-Weg siehe `OPEN_ISSUES.md`.

## Schrift und Stil
- Die Überschriften sollen eine rundliche, freundliche Schrift bekommen, zum Beispiel **Fredoka** oder **Baloo 2**. Beide stehen unter der SIL Open Font License und sind kommerziell frei nutzbar.
- Für Fließtext ist eine gut lesbare Sans-Schrift geplant, zum Beispiel **Nunito**.
- Die Schriften kommen in M3 ins Projekt, zusammen mit dem neuen Menü (UI Toolkit). Der M1-Prototyp nutzt noch die Standardschrift von Unity.
