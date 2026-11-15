using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WorldRush.Core
{
    /// <summary>Spielstand. Klein gehalten; jedes Feld hat einen sicheren Standardwert.</summary>
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public int HighScore;
        public int TotalCoins;
        public int RunsPlayed;
        public int Wallet;
        public int OwnedBoards;
        public int SelectedBoard;
        public int MissionSet = -1;
        public int Mission0, Mission1, Mission2;

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"schemaVersion\":").Append(SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"highScore\":").Append(HighScore.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"totalCoins\":").Append(TotalCoins.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"runsPlayed\":").Append(RunsPlayed.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"wallet\":").Append(Wallet.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"ownedBoards\":").Append(OwnedBoards.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"selectedBoard\":").Append(SelectedBoard.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"missionSet\":").Append(MissionSet.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"mission0\":").Append(Mission0.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"mission1\":").Append(Mission1.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"mission2\":").Append(Mission2.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Liest einen Spielstand. Gibt false zurück, wenn die Datei kaputt oder unbekannt ist.</summary>
        public static bool TryParse(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;
            if (!TryReadInt(trimmed, "schemaVersion", out int version)) return false;
            if (version < 1 || version > CurrentSchemaVersion) return false;

            data = new SaveData { SchemaVersion = CurrentSchemaVersion };
            data.HighScore = ReadNonNegative(trimmed, "highScore");
            data.TotalCoins = ReadNonNegative(trimmed, "totalCoins");
            data.RunsPlayed = ReadNonNegative(trimmed, "runsPlayed");
            data.Wallet = ReadNonNegative(trimmed, "wallet");
            data.OwnedBoards = ReadNonNegative(trimmed, "ownedBoards");
            data.SelectedBoard = ReadNonNegative(trimmed, "selectedBoard");
            data.MissionSet = TryReadInt(trimmed, "missionSet", out int set) ? set : -1;
            data.Mission0 = ReadNonNegative(trimmed, "mission0");
            data.Mission1 = ReadNonNegative(trimmed, "mission1");
            data.Mission2 = ReadNonNegative(trimmed, "mission2");
            if (!BoardShop.Owns(data, data.SelectedBoard)) data.SelectedBoard = 0; // manipulierte Auswahl abfangen
            return true;
        }

        private static int ReadNonNegative(string json, string key) =>
            TryReadInt(json, key, out int value) ? Math.Max(0, value) : 0;

        private static bool TryReadInt(string json, string key, out int value)
        {
            value = 0;
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d{1,10})\\s*[,}]");
            return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>
    /// Speichert lokal als JSON. Schreibt zuerst in eine Temp-Datei und tauscht dann aus,
    /// damit ein Absturz beim Speichern nicht den alten Spielstand zerstört. Eine Sicherung (.bak) bleibt erhalten.
    /// </summary>
    public sealed class LocalSaveRepository
    {
        public LocalSaveRepository(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("filePath required");
            FilePath = filePath;
        }

        public string FilePath { get; }
        public string BackupPath => FilePath + ".bak";

        public SaveData Load()
        {
            if (TryLoad(FilePath, out var data)) return data;
            if (TryLoad(BackupPath, out data)) return data;
            return new SaveData();
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            AtomicFile.WriteAllText(FilePath, data.ToJson());
        }

        private static bool TryLoad(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                return SaveData.TryParse(File.ReadAllText(path), out data);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
