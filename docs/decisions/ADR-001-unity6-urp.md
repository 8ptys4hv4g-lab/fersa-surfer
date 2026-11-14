# ADR-001: Unity 6 + URP + C#

**Datum:** 2026-11-14 · **Status:** angenommen

**Kontext:** Das Spiel soll auf Android, iOS, Windows, macOS, Linux und eventuell im Web laufen, mit echter 3D-Grafik.

**Entscheidung:**
- Engine: Unity 6 LTS (6000.0.x)
- Grafik: Universal Render Pipeline (URP)
- Eingabe: Input System
- Tests: Unity Test Framework

**Begründung:**
- Unity baut für alle Zielplattformen.
- URP läuft auch auf schwächeren Handys.
- C# ist gut testbar.

**Folgen:**
- Für Tests und Builds braucht man einen PC oder Mac mit Unity. Ein Tablet allein reicht nicht.
- Vor der Veröffentlichung müssen die Unity-Lizenzbedingungen geprüft werden.
- Wenn Unity ohne konfigurierte URP öffnet, nutzt der Prototyp automatisch Standard-Materialien.
