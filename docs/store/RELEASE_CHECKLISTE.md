# Release-Checkliste – Google Play (Android)

Reihenfolge von oben nach unten. ✅ = in der App/im Projekt erledigt.

## A. In Unity (ca. 30 Minuten)
1. ✅ Spiel inkl. Menü, Optionen, Tutorial, Credits, DE/EN.
2. Unity Hub → Installationen → Unity 6.6 → *Module hinzufügen* → **Android Build Support** (mit OpenJDK + SDK/NDK).
3. Unity: **File → Build Profiles → Android → Switch Platform**.
4. Menü **Fersa Surfer → 6. Store-Einstellungen + App-Icon (Android)** (Icon, Paketname `com.fersasurfer.game`, Version, IL2CPP, ARM64, .aab, Querformat).
5. **Keystore anlegen:** Edit → Project Settings → Player → Android → *Publishing Settings* → **Keystore Manager** → neu → Passwort → *Add key*.
   - ⚠️ Keystore-Datei + Passwörter **doppelt sichern** (USB-Stick + Passwort-Manager). **Nie** auf GitHub. Ohne ihn keine Updates.
6. Menü **Fersa Surfer → 7. Android-AAB bauen** → Datei `Builds/Android/FersaSurfer-<Version>.aab`.
7. Auf dem eigenen Android-Handy testen (z. B. *Build And Run* mit USB-Debugging oder interner Test in der Play Console).

## B. Google Play Console
1. Entwicklerkonto anlegen (**25 $ einmalig**, Ausweisprüfung).
2. *App erstellen* → Name **Fersa Surfer**, Spiel, kostenlos.
3. **Datenschutzerklärung** online stellen (Text: `docs/store/DATENSCHUTZ.md`, Platzhalter ausfüllen) und Link eintragen.
4. **Datensicherheit**, **Zielgruppe**, **Altersfreigabe** ausfüllen → Antworten in `docs/store/ALTERSFREIGABE_UND_DATENSICHERHEIT.md`.
5. **Store-Eintrag:** Texte aus `docs/store/STORE_TEXTE.md`; Grafiken aus dem Ordner `StoreAssets` (Icon 512×512, Titelbild 1024×500) + **mindestens 2 Screenshots** (Querformat; siehe unten).
6. **Geschlossener Test** anlegen → .aab hochladen → Tester per E-Mail einladen.
   - Neue private Konten: Google verlangt derzeit **mind. 12 Tester über 14 Tage**, bevor „Produktion“ freigeschaltet wird. (Aktuellen Stand in der Play Console prüfen.)
7. Nach dem Test: **Produktion** → Freigabe beantragen → Prüfung durch Google (meist Stunden bis wenige Tage).

## C. Screenshots (vorschlag)
1. Startmenü mit Figur und Level-Auswahl
2. Laufen durch die Stadt mit Münzen
3. Surfen im Kanal (Menü → „Surfen ansehen“)
4. Salto in der Luft
5. Sterne-Bildschirm im Ziel
6. Surfbrett-Shop
→ In Unity: Game-Fenster auf **1920×1080** stellen, ▶, an der Stelle **Pause**, Screenshot (Windows + Shift + S).

## D. Vor jedem Update
- Version in `Assets/_WorldRush/Scripts/Core/ProductInfo.cs` erhöhen (z. B. 0.4.0 → 0.4.1), dann Menü 6 und 7.
- `cd tools/CoreTests && dotnet test` und `tools/UnityCompileCheck/check.sh` müssen sauber sein.
