# ADR-002: Spiellogik ohne Unity-Abhängigkeit

**Datum:** 2026-11-14 · **Status:** angenommen

**Entscheidung:**
- Regeln, Strecke, Kollision, Punkte, Eingabegesten und Speichern liegen in `WorldRush.Core` (`noEngineReferences: true`).
- Kollisionen laufen über feste Boxen statt über die Physik-Engine.

**Begründung:**
- Die Logik ist ohne Unity testbar, mit `dotnet test` auch in der Cloud.
- Sie ist deterministisch, also fair und für spätere Server-Prüfung von Bestwerten geeignet.
- Die Grafik ist austauschbar.

**Folgen:**
- Unity-Objekte haben keine Collider und zeigen nur an.
- Neue Regeln brauchen immer einen Kern-Test.
