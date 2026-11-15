# Rechts- und Sicherheitsprüfung – Fersa Wave

**Stand:** 15.11.2026 · **Hinweis:** Das ist eine sorgfältige technische Prüfung, **keine Rechtsberatung**. Vor dem Verkauf/der Veröffentlichung einmal von einer Anwältin/einem Anwalt (IT-/Markenrecht) gegenlesen lassen.

## 1. Ergebnis der automatischen Prüfung (Repository)
| Prüfung | Ergebnis |
|---|---|
| Nur eigener Code und eigene Doku im Repository | ✅ 51 C#-Dateien, Doku, Workflows – keine fremden Dateien |
| Fremde Bilder/Modelle/Musik/Schriften im Repository | ✅ keine (per `.gitignore` ausgeschlossen) |
| Passwörter, API-Schlüssel, Zertifikate | ✅ keine |
| Namen fremder Spiele/Figuren im Code | ✅ keine |
| Netzwerkverbindungen im Spiel | ✅ keine |

## 2. Urheberrecht – woher stammt was?
| Bestandteil | Herkunft | Lizenz | Status |
|---|---|---|---|
| Spiel-Code, Spiellogik | selbst geschrieben | gehört dir | ✅ |
| Musik und Soundeffekte | per Code erzeugt | gehört dir | ✅ |
| App-Icon, Store-Grafiken | per Code gezeichnet, eigenes Motiv | gehört dir | ✅ |
| Spielfigur (Quaternius „Ultimate Modular Women“) | quaternius.com | **CC0** (frei, auch kommerziell) | ✅ – Lizenzdatei aufbewahren |
| Stadt + Autos (Quaternius) | quaternius.com | **CC0** | ✅ |
| Cartoon City Free (falls genutzt) | Unity Asset Store | Standard Unity Asset Store EULA (Nutzung im Spiel erlaubt, keine Weitergabe der Dateien) | ✅ – Lizenz auf der Store-Seite prüfen |
| HAON SD (Unity-chan, Misaki, Yuko) | Unity Asset Store | Unity-Chan License (Auflagen!) | ⚠️ **nicht verwenden – Paket löschen** (siehe 6.) |
| Unity Engine | Unity Personal | kostenlos bis zur Umsatz-/Finanzierungsgrenze | ✅ – aktuelle Bedingungen prüfen |
| Schrift im Spiel | Unity-Standardschrift | in Unity-Builds erlaubt | ✅ |
| Web-Vorschau | eigener Code + three.js (MIT, per CDN) | MIT | ✅ |

## 3. Marken und Ähnlichkeit zu anderen Spielen
- Spielprinzip „Endless Runner“ ist frei – **keine** fremden Figuren, Logos, Level-Designs, Musik oder UI kopiert. ✅
- Store-Texte nennen **keine** fremden Spiele. **Nie** mit „wie Subway Surfers“ o. Ä. werben. ✅
- **Name „Fersa Wave“:** Keine App/kein Spiel mit diesem Namen gefunden. **Aber:** „FERSA“ ist eine eingetragene Marke eines spanischen Kugellager-Herstellers (Klassen 7 und 12 – Maschinen/Fahrzeugteile). Spiele liegen in anderen Klassen (9, 41) → Risiko eher gering, aber **nicht null**.
  - ➡️ Vor Veröffentlichung: DPMAregister + EUIPO eSearch prüfen, Anwalt kurz fragen, ggf. eigene Marke „Fersa Wave“ in Klassen 9 und 41 anmelden (DPMA ab ca. 290 €). Alternativ einen Namen ohne „Fersa“ wählen.

## 4. Datenschutz und Kinderschutz
| Regel | Status |
|---|---|
| DSGVO: keine personenbezogenen Daten | ✅ nichts wird erhoben/übertragen |
| Kinder (DSGVO Art. 8, COPPA USA) | ✅ keine Daten → keine Einwilligung nötig |
| Keine Werbung, keine Tracker, kein Echtgeld | ✅ |
| Datenschutzerklärung | ✅ Text fertig (`DATENSCHUTZ.md`) – ❌ **online stellen + Kontakt eintragen** |
| Google Play Familien-Richtlinie / Apple Kids-Kategorie | ✅ Voraussetzungen erfüllt (Antworten in `ALTERSFREIGABE_UND_DATENSICHERHEIT.md`) |
| Altersfreigabe (IARC) | ✅ Antworten vorbereitet – erwartet niedrigste Stufe |

## 5. Pflichtangaben als Anbieter
- **Impressum/Kontakt:** In Deutschland müssen Anbieter von Online-Angeboten mit wirtschaftlichem Bezug ein Impressum haben (Name, Anschrift, E-Mail). Google Play und Apple verlangen in der EU außerdem Angaben, ob du „Händler“ bist; dann werden Name/Anschrift öffentlich angezeigt. ➡️ Vorher überlegen (ggf. ladungsfähige Geschäftsadresse statt Wohnadresse).
- **Steuern/Gewerbe:** Solange die App kostenlos ist und nichts verkauft, meist kein Thema. Bei späteren Einnahmen: Gewerbe anmelden, Steuerberatung.

## 6. Technische Sicherheit
| Punkt | Status |
|---|---|
| Spielstand gegen Manipulation geprüft (keine erschummelten Münzen/Bretter) | ✅ getestet |
| Absturzsicheres Speichern mit Sicherungskopie | ✅ getestet |
| Keine Schlüssel in App oder Repository; Keystore/`.p8`/`.ulf` per `.gitignore` gesperrt | ✅ |
| Build: IL2CPP, Code-Stripping, nur HTTPS erlaubt | ✅ |
| Geheimnisse nur als GitHub-Secrets | ✅ Anleitung |
| Eigene Welten werden nur als Daten geladen (kein fremder Code) | ✅ |

## 7. To-do vor der Veröffentlichung (Reihenfolge)
1. **HAON-Paket aus dem Unity-Projekt löschen** (Ordner `Assets/Haons SD series Pack`) – wird nicht genutzt, aber so ist sicher nichts davon im Build.
2. **Lizenzdateien sichern:** Quaternius `License.txt` und ggf. Asset-Store-Rechnung/Lizenz als Nachweis in einem Ordner aufbewahren.
3. **Namen prüfen** (Abschnitt 3).
4. **Datenschutzerklärung + Impressum online** stellen (Name, Anschrift, E-Mail eintragen).
5. **Anwalt kurz gegenlesen lassen**: Name, Datenschutzerklärung, Impressum.
6. Store-Formulare mit den vorbereiteten Antworten ausfüllen.
