# Offene Punkte – Fersa Surfer

| # | Punkt | Wichtigkeit | Wer / Wann |
|---|---|---|---|
| 1 | **Kein GitHub-Repository verbunden:** Der Code liegt nur in der Cloud-Sitzung und geht verloren, wenn sie endet. | 🔴 hoch | Auftraggeber: leeres privates Repo anlegen |
| 2 | Unity-Skripte (Runtime/Editor) und der PlayMode-Test sind nicht in Unity kompiliert oder ausgeführt. | 🔴 hoch | Beim ersten Öffnen in Unity |
| 3 | Paketversionen in `Packages/manifest.json` (URP 17.0.3, Input System 1.11.2, Test Framework 1.4.5) sind ungeprüft. Unity bietet bei Bedarf ein Update an. | 🟡 mittel | Beim ersten Öffnen |
| 4 | URP-Pipeline-Asset wird nicht automatisch angelegt. Ohne URP nutzt der Prototyp Standard-Materialien. Sauber einrichten in M2. | 🟡 mittel | M2 |
| 5 | Grafik ist nur ein Prototyp aus Würfeln und Kapsel. Für M2 braucht es eigene 3D-Modelle und eine animierte Figur: selbst erstellt (Blender), beauftragt oder Assets mit klarer kommerzieller Lizenz. **Kostenfreigabe nötig.** | 🟡 mittel | M2 |
| 6 | Schrift: Fredoka/Nunito (OFL) als Datei einbinden. | 🟢 niedrig | M3 |
| 7 | Markenname „Fersa Surfer“ vor Veröffentlichung prüfen (Markenregister, Stores, Domain). | 🟡 mittel | vor M10 |
| 8 | Git LFS für große 3D-/Audio-Dateien einrichten, sobald solche Dateien dazukommen. | 🟢 niedrig | M2 |
| 9 | Sehr lange Läufe: Die Z-Koordinate wächst unbegrenzt. Bis etwa 20 km ist die Genauigkeit ausreichend. Danach Floating Origin einbauen. | 🟢 niedrig | M8 |
| 10 | Hindernis-Ansichten werden pro Reihe neu erzeugt. Pooling für Mobile ergänzen. | 🟢 niedrig | M8 |
