# Spiel automatisch in der Cloud bauen (ohne selbst Unity zu bedienen)

GitHub baut das Spiel auf seinen Servern:

| Plattform | Ergebnis |
|---|---|
| Android | `.apk` zum Installieren aufs Handy oder Tablet |
| Windows | `.exe` |
| Browser (WebGL) | läuft im Browser auf PC und Tablet |

## Einmalig einrichten (ca. 15 Minuten)

Unity verlangt eine (kostenlose) Lizenz. Die muss **einmal** auf deinem PC erzeugt werden.

1. **Unity Hub** installieren (unity.com/download), mit einem kostenlosen Unity-Konto anmelden und **Unity Personal** aktivieren. Den großen Unity-Editor brauchst du dafür **nicht** zu installieren.
2. Die Lizenzdatei öffnen: `C:\ProgramData\Unity\Unity_lic.ulf`. `ProgramData` ist ein versteckter Ordner, also oben im Explorer `C:\ProgramData\Unity` eintippen. Öffne die Datei mit dem Editor und kopiere den gesamten Inhalt.
3. Auf GitHub im Repository **Settings → Secrets and variables → Actions → New repository secret** wählen. Lege 3 Secrets an:
   - `UNITY_LICENSE`: Inhalt der `.ulf`-Datei
   - `UNITY_EMAIL`: E-Mail deines Unity-Kontos
   - `UNITY_PASSWORD`: Passwort deines Unity-Kontos

   Diese Werte sieht nur GitHub. **Nicht in den Chat schreiben.**

## Bauen

1. Auf GitHub **Actions → „Spiel bauen“ → Run workflow** öffnen und auf **Run workflow** klicken.
2. Nach etwa 20–60 Minuten liegt das Ergebnis unten beim Lauf unter **Artifacts**.
3. **Android:** `FersaSurfer-Android` herunterladen und entpacken. Die `.apk` aufs Handy kopieren und installieren. Dafür musst du „Unbekannte Quellen erlauben“ einschalten.

## Kosten
- Private Repositories haben bei GitHub Free **2.000 Gratis-Minuten pro Monat**.
- Ein kompletter Durchlauf (3 Plattformen) braucht grob **60–150 Minuten**. Gestartet wird nur per Knopfdruck, nicht automatisch.
- Die Kern-Tests laufen bei jedem Hochladen automatisch und brauchen etwa 1 Minute.

## Nicht in der Cloud möglich
- **iPhone/iPad:** braucht einen Apple-Entwickler-Account (99 $/Jahr) und einen Mac-Build-Server. Kommt in M9.
- **Nintendo Switch, PlayStation, Xbox:** Nur mit Zulassung durch den Hersteller (Bewerbung, Vertrag, Entwicklergeräte). Das ist ein Geschäftsschritt für später.

## Hinweis
Der Workflow ist noch **nicht ausgeführt**. Folgende Punkte prüft der erste Lauf:
- die Unity-Version (aus `ProjectSettings/ProjectVersion.txt`, aktuell 6000.6.9f1)
- die Paketversionen
- ob das Skript `CiBuild.cs` kompiliert

Fehler daraus behebt Claude.
