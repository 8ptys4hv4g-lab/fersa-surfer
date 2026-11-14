# ADR-003: UI im Prototyp per IMGUI, ab M3 UI Toolkit

**Datum:** 2026-11-14 · **Status:** angenommen

**Entscheidung:**
- Der M1-Prototyp nutzt IMGUI (`OnGUI`). Dafür braucht es keine Assets oder Szenen, und es ist sofort lauffähig.
- Ab M3 kommt **UI Toolkit** mit eigener OFL-Schrift (Fredoka/Nunito) und Safe-Area-Unterstützung.

**Begründung:** Schnell spielbar werden, ohne ungeprüfte UI-Dateien von Hand zu schreiben.

**Folgen:** `HudOverlay` ist eine Wegwerf-Komponente.
