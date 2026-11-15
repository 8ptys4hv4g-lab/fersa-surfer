using System;
using System.Collections.Generic;
using System.Globalization;

namespace WorldRush.Core
{
    public enum Language { German = 0, English = 1 }

    /// <summary>Alle Texte des Spiels auf Deutsch und Englisch. Aufruf: L.T("key") oder L.T("key", wert).</summary>
    public static class L
    {
        public static Language Current = Language.German;

        private static readonly Dictionary<string, (string de, string en)> Texts = new Dictionary<string, (string, string)>
        {
            ["tab.start"] = ("Start", "Play"),
            ["tab.missions"] = ("Aufgaben", "Missions"),
            ["tab.shop"] = ("Shop", "Shop"),
            ["tab.settings"] = ("Optionen", "Options"),
            ["menu.controls"] = ("Pfeiltasten oder Wischen: ← → Spur · ↑ Springen · ↓ Rutschen", "Arrow keys or swipe: ← → lane · ↑ jump · ↓ slide"),
            ["menu.lastRun"] = ("Letzte Runde: {0} m · {1} Münzen · {2} Punkte", "Last run: {0} m · {1} coins · {2} points"),
            ["menu.outfit"] = ("Dein Outfit, in eigenen Worten:", "Your outfit, in your own words:"),
            ["menu.wear"] = ("Anziehen", "Wear"),
            ["menu.character"] = ("Figur: {0}", "Character: {0}"),
            ["menu.level"] = ("Level {0} · {1}", "Level {0} · {1}"),
            ["menu.play"] = ("Spielen", "Play"),
            ["menu.surfPreview"] = ("Surfen ansehen", "Try surfing"),
            ["hud.coinsBest"] = ("Münzen {0}    Best {1}", "Coins {0}    Best {1}"),
            ["hud.combo"] = ("Combo ×{0}", "Combo ×{0}"),
            ["hud.pause"] = ("Pause", "Pause"),
            ["hud.resume"] = ("Weiter", "Resume"),
            ["hud.goal"] = ("Ziel", "Goal"),
            ["hud.surfHint"] = ("Surfen! In der Luft nochmal springen = Salto", "Surfing! Jump again in the air = flip"),
            ["power.board"] = ("Surfboard", "Hoverboard"),
            ["power.magnet"] = ("Magnet", "Magnet"),
            ["power.shield"] = ("Schild", "Shield"),
            ["power.double"] = ("2× Punkte", "2× Points"),
            ["popup.combo"] = ("Combo ×{0}!", "Combo ×{0}!"),
            ["popup.trick"] = ("Salto! +{0}", "Flip! +{0}"),
            ["popup.shield"] = ("Schild!", "Shield!"),
            ["popup.mission"] = ("Aufgabe geschafft! +{0} Münzen", "Mission complete! +{0} coins"),
            ["over.title"] = ("Autsch!", "Ouch!"),
            ["over.best"] = ("Neuer Bestwert!", "New best!"),
            ["over.stats"] = ("{0} m · {1} Münzen · {2} Punkte", "{0} m · {1} coins · {2} points"),
            ["over.retry"] = ("Nochmal", "Retry"),
            ["won.perfect"] = ("Perfekt!", "Perfect!"),
            ["won.done"] = ("Geschafft!", "Well done!"),
            ["won.stats"] = ("{0}/{1} Münzen · {2} Saltos · {3} Punkte", "{0}/{1} coins · {2} flips · {3} points"),
            ["won.next"] = ("Nächstes Level", "Next level"),
            ["won.menu"] = ("Menü", "Menu"),
            ["missions.title"] = ("Aufgaben für heute", "Today's missions"),
            ["missions.wallet"] = ("Dein Konto: {0} Münzen", "Your coins: {0}"),
            ["missions.done"] = ("Geschafft!", "Done!"),
            ["shop.title"] = ("Surfbrett-Shop", "Board shop"),
            ["shop.selected"] = ("Ausgewählt", "Selected"),
            ["shop.select"] = ("Wählen", "Select"),
            ["shop.price"] = ("{0} Münzen", "{0} coins"),
            ["shop.bought"] = ("Gekauft!", "Bought!"),
            ["shop.tooPoor"] = ("Zu wenig Münzen", "Not enough coins"),
            ["shop.note"] = ("Nur Spiel-Münzen – kein echtes Geld.", "Game coins only – no real money."),
            ["settings.title"] = ("Optionen", "Options"),
            ["settings.music"] = ("Musik", "Music"),
            ["settings.sfx"] = ("Geräusche", "Sounds"),
            ["settings.quality"] = ("Grafik", "Graphics"),
            ["settings.language"] = ("Sprache", "Language"),
            ["settings.tutorial"] = ("Tutorial wiederholen", "Replay tutorial"),
            ["settings.credits"] = ("Credits & Lizenzen", "Credits & licences"),
            ["settings.back"] = ("Zurück", "Back"),
            ["quality.0"] = ("Sparsam", "Low"),
            ["quality.1"] = ("Mittel", "Medium"),
            ["quality.2"] = ("Hoch", "High"),
            ["lang.name"] = ("Deutsch", "English"),
            ["tab.world"] = ("Welt", "World"),
            ["world.title"] = ("Welt bauen", "Build a world"),
            ["world.tree"] = ("Baum", "Tree"),
            ["world.house"] = ("Haus", "House"),
            ["world.garden"] = ("Garten", "Garden"),
            ["world.undo"] = ("Rückgängig", "Undo"),
            ["world.clear"] = ("Leeren", "Clear"),
            ["world.sample"] = ("Beispiel", "Example"),
            ["world.build"] = ("In 3D bauen", "Build in 3D"),
            ["world.standard"] = ("Standard-Stadt", "Default city"),
            ["world.hint"] = ("Male neben die Straße: Baum = tippen oder Linie · Haus = Rechteck ziehen · Garten = Umriss", "Draw next to the road: tree = tap or line · house = drag a box · garden = outline"),
            ["world.built"] = ("Deine Welt ist fertig! Tab „Start“ → Spielen.", "Your world is ready! Tab \"Play\" → Play."),
            ["world.empty"] = ("Zeichne zuerst etwas.", "Draw something first."),
            ["world.active"] = ("Eigene Welt aktiv", "Custom world active"),
            ["world.standardOn"] = ("Standard-Stadt aktiv", "Default city active"),
            ["tut.lane"] = ("Wand! Wisch zur Seite (← →)", "Wall! Swipe sideways (← →)"),
            ["tut.jump"] = ("Hürde! Wisch nach oben (↑)", "Hurdle! Swipe up (↑)"),
            ["tut.slide"] = ("Balken! Wisch nach unten (↓)", "Bar! Swipe down (↓)"),
            ["tut.trick"] = ("Wasser! Springen, dann in der Luft nochmal = Salto", "Water! Jump, then jump again mid-air = flip"),
            ["tut.done"] = ("Super! Du kannst alles. Viel Spaß!", "Great! You've got it. Have fun!"),
            ["credits.text"] = (
                "Fersa Surfer\n\nSpiel & Code: Fersa Surfer Team\nSpielfiguren & Stadt: Quaternius (CC0) bzw. Unity-Asset-Store-Pakete (Standard Unity Asset Store EULA)\nMusik & Sounds: im Spiel erzeugt\nEngine: Unity\n\nKeine Werbung · keine Tracker · keine Datenübertragung",
                "Fersa Surfer\n\nGame & code: Fersa Surfer team\nCharacters & city: Quaternius (CC0) or Unity Asset Store packages (Standard Unity Asset Store EULA)\nMusic & sounds: generated in-game\nEngine: Unity\n\nNo ads · no trackers · no data sharing"),
        };

        public static IEnumerable<string> Keys => Texts.Keys;

        public static string T(string key, params object[] args)
        {
            if (!Texts.TryGetValue(key, out var pair)) return key;
            string text = Current == Language.English ? pair.en : pair.de;
            return args == null || args.Length == 0 ? text : string.Format(CultureInfo.InvariantCulture, text, args);
        }

        public static string Raw(string key, Language language) =>
            Texts.TryGetValue(key, out var pair) ? (language == Language.English ? pair.en : pair.de) : key;
    }
}
