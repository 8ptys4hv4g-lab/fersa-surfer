# Offene Punkte – Fesara

| # | Punkt | Wichtigkeit | Wer / Wann |
|---|---|---|---|
| 1 | ~~Kein GitHub-Repository~~ → erledigt: github.com/8ptys4hv4g-lab/fersa-surfer | ✅ | – |
| 2 | Unity-Skripte (Runtime/Editor) und der PlayMode-Test sind nicht in Unity kompiliert oder ausgeführt. | 🔴 hoch | Beim ersten Öffnen in Unity |
| 3 | Paketversionen in `Packages/manifest.json` (URP 17.0.3, Input System 1.11.2, Test Framework 1.4.5) sind ungeprüft. Unity bietet bei Bedarf ein Update an. | 🟡 mittel | Beim ersten Öffnen |
| 4 | URP-Pipeline-Asset wird nicht automatisch angelegt. Ohne URP nutzt der Prototyp Standard-Materialien. Sauber einrichten in M2. | 🟡 mittel | M2 |
| 5 | Grafik ist nur ein Prototyp aus Würfeln und Kapsel. Für M2 braucht es eigene 3D-Modelle und eine animierte Figur: selbst erstellt (Blender), beauftragt oder Assets mit klarer kommerzieller Lizenz. **Kostenfreigabe nötig.** | 🟡 mittel | M2 |
| 6 | Schrift: Fredoka/Nunito (OFL) als Datei einbinden. | 🟢 niedrig | M3 |
| 7 | Markenname „Fesara“: Internetsuche erledigt (frei für Spiele). Offen: tmdn.org/tmview selbst prüfen, Anwalt, Markenanmeldung; Domain z. B. fesara-game.com. | 🟡 mittel | vor Veröffentlichung |
| 8 | Git LFS für große 3D-/Audio-Dateien einrichten, sobald solche Dateien dazukommen. | 🟢 niedrig | M2 |
| 9 | Sehr lange Läufe: Die Z-Koordinate wächst unbegrenzt. Bis etwa 20 km ist die Genauigkeit ausreichend. Danach Floating Origin einbauen. | 🟢 niedrig | M8 |
| 10 | Hindernis-Ansichten werden pro Reihe neu erzeugt. Pooling für Mobile ergänzen. | 🟢 niedrig | M8 |
| 11 | Cloud-Build braucht 3 GitHub-Secrets (Unity-Lizenz). Erster Lauf ist ungeprüft (Unity-Version 6000.6.9f1). | 🔴 hoch | Auftraggeber: Secrets anlegen |
| 12 | Nintendo Switch, PlayStation, Xbox: nur mit Zulassung durch den Hersteller. | 🟢 später | Geschäftsentscheidung |
| 13 | iOS: Apple Developer Program (99 $/Jahr) + macOS-Build. | 🟡 mittel | M9, Kostenfreigabe |
