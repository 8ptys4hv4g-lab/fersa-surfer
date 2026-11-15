# AGENTS.md – Regeln für KI-Assistenten (Claude Code und andere)

## Projekt
**Fesara** ist ein 3D-Endless-Runner in Unity 6 mit URP und C#. Der interne Code-Name lautet `WorldRush`.
Die verbindliche Vision steht in `docs/archive/2026-11-14_MASTERPLAN_v1.0.md`, der aktuelle Plan in `docs/PLAN.md`.

## Vor jeder Arbeit lesen
1. `docs/PROJECT_STATE.md` (wo wir stehen)
2. `docs/PLAN.md` (was als Nächstes kommt)
3. `docs/OPEN_ISSUES.md` (bekannte Probleme)

## Nach jeder Arbeit aktualisieren
- `docs/PROJECT_STATE.md`: Stand, Tests und was nicht getestet ist
- `CHANGELOG.md`: Eintrag unter „Unveröffentlicht“
- `docs/OPEN_ISSUES.md`: neue oder gelöste Punkte
- Wichtige Technikentscheidung: neue ADR in `docs/decisions/`
- Ersetzter Plan: alte Version nach `docs/archive/JJJJ-MM-TT_NAME.md` verschieben, **nie löschen**

## Code-Regeln
- **Spiellogik gehört in `Assets/_WorldRush/Scripts/Core/`.** Dort gibt es kein `UnityEngine` (`noEngineReferences: true`), C# 9 und .NET Standard 2.1.
- Unity-Darstellung kommt nach `Scripts/Runtime/`, Editor-Werkzeuge nach `Scripts/Editor/`.
- Jede Regeländerung im Kern braucht einen Test in `Tests/EditMode/`.
- Kern-Tests laufen ohne Unity:

```bash
cd tools/CoreTests && dotnet test
```

  Sie müssen grün sein.
- **Vor jedem Push von Unity-Code:** `tools/UnityCompileCheck/check.sh` ausführen (kompiliert Runtime + Editor gegen Unity-Referenzen). Erwartet sind nur Meldungen zu APIs, die neuer als Unity 2021 sind; alle anderen Fehler müssen behoben sein.
- Keine handgeschriebenen `.unity`-, `.prefab`- oder `.mat`-Dateien. Solche Dateien erzeugt Unity selbst, zum Beispiel über Editor-Menüs.
- Neue Pakete nur mit klarer Lizenz. Keine kostenpflichtigen Dienste oder Assets ohne Freigabe.
- **Keine API-Schlüssel und Passwörter im Repository oder in der App.**
- Keine Kopie fremder Spiele: Figuren, Level, Grafik, Musik, Marken und Schriften sind eigen oder korrekt lizenziert.

## Ehrlichkeit
- „Kompiliert“ nur sagen, wenn Unity oder `dotnet` es wirklich kompiliert hat.
- „Spielbar“ oder „auf Gerät getestet“ nur sagen, wenn es wirklich gestartet wurde.
- Statusberichte folgen dem Format in `docs/PROJECT_STATE.md`.

## Sprache
Mit dem Auftraggeber auf Deutsch kommunizieren, kurz und ohne Fachchinesisch. Code und Bezeichner bleiben auf Englisch, Kommentare dürfen deutsch sein.

## Modellwahl (Empfehlung)
| Aufgabe | Modell | Denkstufe |
|---|---|---|
| Doku, kleine Änderungen, Tests | Sonnet | Low–Medium |
| Normale Features (Menüs, Outfit-Katalog) | Sonnet oder Opus | Medium |
| Welt-Generator, Architektur, schwierige Fehler | Opus | High |
