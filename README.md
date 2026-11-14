# Fersa Surfer

Ein 3D-Endless-Runner mit eigenen Welten und Outfits.
Der interne Code-Name ist **WorldRush**, so heißen auch die Ordner und Namespaces.

**Status:** M0 + M1 als **vorbereiteter Unity-Projektstand**. Die Spiellogik ist mit 55 automatischen Tests geprüft. In Unity wurde das Projekt noch nicht geöffnet. Details stehen in [`docs/PROJECT_STATE.md`](docs/PROJECT_STATE.md).

## Schnellstart (Windows oder Mac)

1. **Unity Hub** installieren und darüber **Unity 6 LTS (6000.0.x)** mit den Modulen *Windows/Mac Build Support (IL2CPP)* und *Android Build Support* installieren.
2. Im Unity Hub auf **Add → Add project from disk** klicken und diesen Ordner wählen.
3. Beim ersten Öffnen lädt Unity die Pakete. Das dauert ein paar Minuten.
4. Im Menü oben **Fersa Surfer → 1. Demo-Szene erstellen** wählen.
5. Oben auf **▶ Play** drücken.

**Steuerung:**

| Aktion | PC | Handy/Tablet |
|---|---|---|
| Spur wechseln | ← → oder A D | links/rechts wischen |
| Springen | ↑, W oder Leertaste | nach oben wischen |
| Rutschen | ↓ oder S | nach unten wischen |
| Pause | Esc oder P | Pause-Knopf |

Auch mit gedrückter Maus kannst du wischen.

## Ohne eigenes Unity: in der Cloud bauen
Siehe [`docs/BUILD_IN_DER_CLOUD.md`](docs/BUILD_IN_DER_CLOUD.md). GitHub baut dort Android, Windows und Browser auf Knopfdruck.

## Android-Testbuild

1. **File → Build Profiles → Android → Switch Platform** wählen.
2. **Fersa Surfer → 2. Sichere Build-Einstellungen setzen** wählen.
3. Am Handy den Entwicklermodus und USB-Debugging einschalten und das Handy per USB anschließen.
4. **Build And Run** klicken.

## Tests

- **In Unity:** Über **Window → General → Test Runner** die Tests unter *EditMode* und *PlayMode* starten.
- **Ohne Unity (nur die Spiellogik):**

```bash
cd tools/CoreTests && dotnet test
```

## Wichtige Dokumente

| Datei | Wofür |
|---|---|
| [`docs/PLAN.md`](docs/PLAN.md) | Roadmap M0–M11 mit Status |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Wie der Code aufgebaut ist |
| [`docs/PROJECT_STATE.md`](docs/PROJECT_STATE.md) | Aktueller Stand: was fertig ist und was getestet wurde |
| [`docs/OPEN_ISSUES.md`](docs/OPEN_ISSUES.md) | Offene Punkte und Risiken |
| [`docs/SECURITY.md`](docs/SECURITY.md) | Schutz gegen Hacken und Schummeln, Datenschutz |
| [`CHANGELOG.md`](CHANGELOG.md) | Änderungen pro Version |
| [`AGENTS.md`](AGENTS.md) / [`CLAUDE.md`](CLAUDE.md) | Regeln für KI-Assistenten |
| [`docs/archive/`](docs/archive/) | Alte Versionen von Plänen (nicht löschen) |
| [`docs/decisions/`](docs/decisions/) | Technische Entscheidungen (ADRs) |
