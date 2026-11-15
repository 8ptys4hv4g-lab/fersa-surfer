# Sicherheit, Schutz vor Schummeln und Datenschutz

**Ehrlich vorweg:** Keine App ist „unhackbar“. Alles, was auf dem Handy des Spielers läuft, kann ein entschlossener Angreifer verändern. Das Ziel ist deshalb:
1. Es gibt **nichts Wertvolles** in der App zu stehlen, also keine Schlüssel und keine Passwörter.
2. **Alles, was Geld kostet oder fair sein muss**, prüft später ein eigener Server.
3. Manipulation ist **aufwendig** und richtet **keinen Schaden** bei anderen an.

## Schon umgesetzt (M1)

| Maßnahme | Wo | Schützt vor |
|---|---|---|
| Spielstand wird geprüft: Version, Format, keine negativen Werte | `LocalSaveRepository` | Abstürze durch kaputte oder manipulierte Dateien |
| Atomares Speichern mit Sicherung (.bak) | `LocalSaveRepository` | Datenverlust bei Absturz oder leerem Akku |
| Keine Schlüssel und Passwörter im Code; `.gitignore` blockt Keystores und `.env` | Repository | Gestohlene Zugänge |
| Eigener, deterministischer Zufall mit fester Kollisionslogik | `Core` | Unfaire Strecken; später überprüfbare Bestwerte |
| Build-Einstellungen: IL2CPP, Code-Stripping, nur HTTPS | Menü „Fesara → 2.“ | Einfaches Auslesen oder Ändern des Codes, unsichere Verbindungen |
| Keine Internetverbindung im Spiel | gesamt | Keine Angriffsfläche über das Netz |

## Später, wenn Online-Funktionen kommen (M10/M11)

| Thema | Regel |
|---|---|
| **Käufe** | Nur über Apple App Store und Google Play. Kaufbelege prüft **unser Server** beim Store, nie die App allein. |
| **Bestenlisten** | Der Server prüft Ergebnisse auf Plausibilität (Tempo, Zeit, Seed). Möglich, weil die Logik deterministisch ist. |
| **KI-Funktionen** | App → unser Server (mit Login, Limits, Kostenbremse) → KI-Anbieter. **Keine KI-Schlüssel in der App.** |
| **Eigene Welten und Outfits** | Werden nur als **Daten** (JSON) geladen, nie als Code. Strenge Prüfung von Größe, Anzahl und erlaubten Bausteinen. |
| **Teilen / Community** | Erst mit Moderation, Meldefunktion und Löschung. |
| **Verbindungen** | Nur HTTPS. Certificate Pinning prüfen. |
| **Erkennung von Manipulation** | Optional: Play Integrity API (Android) und App Attest (iOS) vor wichtigen Server-Aktionen. |
| **Verschleierung** | Optional ein Obfuscator für C#. Er bremst nur, schützt aber nicht vollständig. |

## Datenschutz (DSGVO)
- Version 1 sammelt **keine persönlichen Daten**. Der Spielstand liegt nur lokal auf dem Gerät.
- Analytics erst ab M10: datensparsam, mit Einwilligung, abschaltbar und löschbar.
- Bei Kindern und Jugendlichen gilt: keine personalisierte Werbung, Kaufschutz und eine passende Altersfreigabe.
- Vor der Veröffentlichung braucht es eine Datenschutzerklärung und eine rechtliche Prüfung. **Das ersetzt diese Datei nicht.**

## Fehler melden
Sicherheitsprobleme bitte nicht öffentlich posten, sondern direkt dem Projektinhaber melden.
