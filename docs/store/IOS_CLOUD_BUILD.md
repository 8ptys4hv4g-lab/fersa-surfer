# iPhone/iPad ohne Mac: Bauen in der Cloud → TestFlight

GitHub baut die App: ein Linux-Server erzeugt mit Unity das Xcode-Projekt, ein Mac-Server von GitHub signiert sie und lädt sie zu Apple hoch. Du drückst nur **Run workflow**.

> Status: vorbereitet, **noch nicht ausgeführt** (braucht dein Apple-Konto). Beim ersten Lauf können Korrekturen nötig sein – dann den Fehlertext aus GitHub → Actions an Claude geben.

## Einmalig einrichten (ca. 30–45 Minuten)

### 1. Apple Developer Program (99 $/Jahr)
developer.apple.com → **Account** → **Enroll** → als Einzelperson mit deiner Apple-ID (Zwei-Faktor-Anmeldung nötig). Die Freischaltung kann 1–2 Tage dauern.

### 2. App-ID anlegen
developer.apple.com → **Certificates, Identifiers & Profiles** → **Identifiers** → **+** → *App IDs* → *App* → Beschreibung „Fesara“, Bundle ID **explicit**: `com.fesara.game` → Registrieren.

### 3. App in App Store Connect anlegen
appstoreconnect.apple.com → **Apps** → **+** → *Neue App* → Plattform iOS, Name „Fesara“, Sprache Deutsch, Bundle-ID `com.fesara.game`, SKU z. B. `fesara01`.

### 4. API-Schlüssel erzeugen (für den automatischen Upload)
App Store Connect → **Benutzer und Zugriff** → **Integrationen** → **App Store Connect API** → *Team-Schlüssel* → **+** → Name „GitHub“, Zugriff **Admin** → Erstellen.
- **Schlüssel herunterladen** (`AuthKey_XXXX.p8`) – geht **nur einmal**! Sicher aufbewahren.
- Notieren: **Key ID** (in der Liste) und **Issuer ID** (oben auf der Seite).

### 5. Team-ID finden
developer.apple.com → **Account** → **Membership details** → **Team ID** (10 Zeichen).

### 6. Unity-Lizenz-Datei
Datei `C:\ProgramData\Unity\Unity_lic.ulf` mit dem Editor öffnen und den **ganzen Inhalt** kopieren. (Fehlt sie: Unity Hub → ⚙️ → Lizenzen → Personal-Lizenz hinzufügen.)

### 7. Secrets auf GitHub eintragen
github.com/8ptys4hv4g-lab/fersa-surfer → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**:

| Name | Inhalt |
|---|---|
| `UNITY_LICENSE` | Inhalt der `.ulf`-Datei |
| `UNITY_EMAIL` | E-Mail deines Unity-Kontos |
| `UNITY_PASSWORD` | Passwort deines Unity-Kontos |
| `APPLE_TEAM_ID` | Team-ID aus Schritt 5 |
| `ASC_KEY_ID` | Key ID aus Schritt 4 |
| `ASC_ISSUER_ID` | Issuer ID aus Schritt 4 |
| `ASC_KEY_P8` | **gesamter Inhalt** der `.p8`-Datei (mit `-----BEGIN PRIVATE KEY-----` …) |

Diese Werte sieht nur GitHub. **Nie in den Chat oder in Dateien schreiben.**

## Bauen
GitHub → **Actions** → **„iPhone/iPad bauen → TestFlight“** → **Run workflow**. Dauer ca. 40–70 Minuten.
Danach in App Store Connect → **TestFlight**: Build erscheint nach der Verarbeitung → dich selbst als Tester hinzufügen → App **TestFlight** auf iPhone/iPad installieren → Fesara testen.

## Kosten bei GitHub
Private Projekte: macOS-Minuten zählen **10-fach** (2.000 Gratis-Minuten ≈ 200 Mac-Minuten/Monat). Ein Durchlauf braucht ca. 15–25 Mac-Minuten + Linux-Minuten → einige Builds pro Monat sind gratis. Speicher für Zwischendateien wird nach 1 Tag gelöscht.

## Später: Veröffentlichen
App Store Connect → App → Version: Texte (`docs/store/STORE_TEXTE.md`), Screenshots (iPhone + iPad in den dort verlangten Größen), Datenschutz-Angaben „Keine Daten erfasst“, Altersfreigabe, Datenschutz- und Support-URL → Build auswählen → **Zur Prüfung einreichen**.
