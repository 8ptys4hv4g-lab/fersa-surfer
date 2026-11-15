using System;
using WorldRush.Core;
using System.Collections.Generic;
using System.Text;

namespace WorldRush.Core.Outfits
{
    /// <summary>
    /// Der aktuelle Look als Daten: Farbe pro Bereich und ob ein Kleid getragen wird.
    /// Wird in einen ganz normalen Outfit-Satz übersetzt („blonde Haare, rotes Kleid, …“), damit Antippen
    /// und Eintippen genau gleich funktionieren.
    /// </summary>
    public sealed class OutfitSpec
    {
        public string Hair = "blonde";
        public string Top = "white";
        public string Bottom = "blue";
        public string Dress = "red";
        public string Shoes = "brown";
        public bool WearsDress = true;

        public OutfitSpec Clone() => (OutfitSpec)MemberwiseClone();

        public string Get(OutfitSlot slot) => slot switch
        {
            OutfitSlot.Hair => Hair,
            OutfitSlot.Top => Top,
            OutfitSlot.Bottom => Bottom,
            OutfitSlot.Dress => Dress,
            OutfitSlot.Shoes => Shoes,
            _ => null
        };

        public void Set(OutfitSlot slot, string color)
        {
            if (!OutfitStudio.IsKnownColor(color)) return;
            switch (slot)
            {
                case OutfitSlot.Hair: Hair = color; break;
                case OutfitSlot.Top: Top = color; WearsDress = false; break;
                case OutfitSlot.Bottom: Bottom = color; WearsDress = false; break;
                case OutfitSlot.Dress: Dress = color; WearsDress = true; break;
                case OutfitSlot.Shoes: Shoes = color; break;
            }
        }

        /// <summary>Übernimmt alles, was im Text erkannt wurde; Rest bleibt wie vorher.</summary>
        public static OutfitSpec FromText(string text, OutfitSpec fallback = null)
        {
            var spec = fallback?.Clone() ?? new OutfitSpec();
            var parsed = OutfitTextParser.Parse(text);
            foreach (var w in parsed.Wishes)
            {
                if (w.Color == null) { if (w.Slot == OutfitSlot.Dress) spec.WearsDress = true; else if (w.Slot == OutfitSlot.Top || w.Slot == OutfitSlot.Bottom) spec.WearsDress = false; continue; }
                spec.Set(w.Slot, w.Color);
            }
            return spec;
        }

        public string ToText(Language language)
        {
            var parts = new List<string> { OutfitStudio.Phrase(OutfitSlot.Hair, Hair, language) };
            if (WearsDress) parts.Add(OutfitStudio.Phrase(OutfitSlot.Dress, Dress, language));
            else
            {
                parts.Add(OutfitStudio.Phrase(OutfitSlot.Top, Top, language));
                parts.Add(OutfitStudio.Phrase(OutfitSlot.Bottom, Bottom, language));
            }
            parts.Add(OutfitStudio.Phrase(OutfitSlot.Shoes, Shoes, language));
            var sb = new StringBuilder(string.Join(", ", parts));
            if (sb.Length > 0) sb[0] = char.ToUpperInvariant(sb[0]);
            return sb.ToString();
        }

        public bool SameAs(OutfitSpec o) => o != null && Hair == o.Hair && Shoes == o.Shoes && WearsDress == o.WearsDress
            && (WearsDress ? Dress == o.Dress : Top == o.Top && Bottom == o.Bottom);
    }

    public sealed class OutfitPreset
    {
        public OutfitPreset(string key, OutfitSpec spec) { Key = key; Spec = spec; }
        /// <summary>Übersetzungsschlüssel des Namens, z. B. "look.mermaid".</summary>
        public string Key { get; }
        public OutfitSpec Spec { get; }
    }

    /// <summary>Farben zum Antippen, fertige Looks und „Würfeln“ – kinderleicht, ohne Tippen.</summary>
    public static class OutfitStudio
    {
        public static readonly string[] HairColors = { "blonde", "brown", "black", "red", "pink", "purple", "turquoise", "silver" };
        public static readonly string[] ClothColors = { "red", "pink", "purple", "blue", "turquoise", "green", "yellow", "orange", "white", "black" };
        public static readonly string[] ShoeColors = { "white", "black", "brown", "red", "pink", "blue", "turquoise", "gold" };

        public static string[] ColorsFor(OutfitSlot slot) => slot == OutfitSlot.Hair ? HairColors : slot == OutfitSlot.Shoes ? ShoeColors : ClothColors;

        private static readonly HashSet<string> Known = new HashSet<string>(
            new[] { "red", "blue", "green", "yellow", "black", "white", "brown", "blonde", "pink", "purple", "orange", "grey", "silver", "gold", "beige", "turquoise" });

        public static bool IsKnownColor(string id) => id != null && Known.Contains(id);

        public static readonly OutfitPreset[] Presets =
        {
            new OutfitPreset("look.mermaid", new OutfitSpec { Hair = "red", Dress = "turquoise", Shoes = "gold", WearsDress = true }),
            new OutfitPreset("look.hero", new OutfitSpec { Hair = "black", Top = "red", Bottom = "blue", Shoes = "red", WearsDress = false }),
            new OutfitPreset("look.neon", new OutfitSpec { Hair = "pink", Top = "turquoise", Bottom = "black", Shoes = "white", WearsDress = false }),
            new OutfitPreset("look.sunny", new OutfitSpec { Hair = "blonde", Dress = "yellow", Shoes = "white", WearsDress = true }),
            new OutfitPreset("look.princess", new OutfitSpec { Hair = "blonde", Dress = "pink", Shoes = "gold", WearsDress = true }),
            new OutfitPreset("look.galaxy", new OutfitSpec { Hair = "purple", Top = "black", Bottom = "purple", Shoes = "silver", WearsDress = false }),
        };

        // Farbpaare, die gut zusammenpassen (Oberteil/Kleid, Hose)
        private static readonly (string a, string b)[] Pairs =
        {
            ("turquoise", "white"), ("pink", "purple"), ("yellow", "blue"), ("red", "black"), ("white", "blue"),
            ("orange", "turquoise"), ("purple", "black"), ("green", "yellow"), ("pink", "white"), ("blue", "orange")
        };

        /// <summary>Zufälliger, aber stimmiger Look (gleicher Startwert = gleicher Look, gut für Tests).</summary>
        public static OutfitSpec Roll(int seed)
        {
            var r = new DeterministicRandom(unchecked(seed * 31 + 17));
            var pair = Pairs[r.Range(0, Pairs.Length)];
            bool dress = r.NextFloat() < 0.45f;
            return new OutfitSpec
            {
                Hair = HairColors[r.Range(0, HairColors.Length)],
                Dress = pair.a,
                Top = pair.a,
                Bottom = pair.b,
                Shoes = ShoeColors[r.Range(0, ShoeColors.Length)],
                WearsDress = dress
            };
        }

        // ---------- Sprache ----------

        private static readonly Dictionary<string, (string stem, bool invariable, string en)> Words = new Dictionary<string, (string, bool, string)>
        {
            { "red", ("rot", false, "red") }, { "blue", ("blau", false, "blue") }, { "green", ("grün", false, "green") },
            { "yellow", ("gelb", false, "yellow") }, { "black", ("schwarz", false, "black") }, { "white", ("weiß", false, "white") },
            { "brown", ("braun", false, "brown") }, { "blonde", ("blond", false, "blonde") }, { "pink", ("pink", false, "pink") },
            { "purple", ("lila", true, "purple") }, { "orange", ("orang", false, "orange") }, { "grey", ("grau", false, "grey") },
            { "silver", ("silbern", false, "silver") }, { "gold", ("golden", false, "gold") }, { "beige", ("beig", false, "beige") },
            { "turquoise", ("türkis", false, "turquoise") }
        };

        /// <summary>Farbname allein, z. B. "rot" / "red" (für Rückmeldungen).</summary>
        public static string ColorName(string id, Language language)
        {
            if (id == null || !Words.TryGetValue(id, out var w)) return "-";
            if (language == Language.English) return w.en;
            if (w.invariable) return w.stem;
            return id == "orange" ? "orange" : id == "silver" ? "silber" : id == "gold" ? "gold" : id == "beige" ? "beige" : w.stem;
        }

        /// <summary>„rotes Kleid“, „rote Hose“, „blonde Haare“ bzw. "red dress".</summary>
        public static string Phrase(OutfitSlot slot, string color, Language language)
        {
            if (!Words.TryGetValue(color ?? "", out var w)) w = Words["white"];
            if (language == Language.English)
            {
                string enNoun = slot switch { OutfitSlot.Hair => "hair", OutfitSlot.Dress => "dress", OutfitSlot.Top => "top", OutfitSlot.Bottom => "pants", _ => "shoes" };
                return w.en + " " + enNoun;
            }
            // Endung nach Geschlecht/Zahl: Haare/Schuhe (Mehrzahl) und Hose (weiblich) → -e, Kleid/Oberteil (sächlich) → -es
            (string noun, string ending) = slot switch
            {
                OutfitSlot.Hair => ("Haare", "e"),
                OutfitSlot.Dress => ("Kleid", "es"),
                OutfitSlot.Top => ("Oberteil", "es"),
                OutfitSlot.Bottom => ("Hose", "e"),
                _ => ("Schuhe", "e")
            };
            string adj = w.invariable ? w.stem : w.stem + ending;
            return adj + " " + noun;
        }
    }
}
