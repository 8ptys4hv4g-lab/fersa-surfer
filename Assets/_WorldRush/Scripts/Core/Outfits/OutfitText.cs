using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldRush.Core.Outfits
{
    public enum OutfitSlot { Hair, Top, Bottom, Dress, Shoes, Accessory }

    /// <summary>Ein erkannter Wunsch, z. B. Slot=Dress, Type="dress", Color="red".</summary>
    public sealed class OutfitWish
    {
        public OutfitWish(OutfitSlot slot, string type, string color)
        {
            Slot = slot;
            Type = type;
            Color = color;
        }

        public OutfitSlot Slot { get; }
        public string Type { get; }
        /// <summary>Farb-ID (englisch, z. B. "red") oder null, wenn keine Farbe genannt wurde.</summary>
        public string Color { get; }

        public override string ToString() => $"{Slot}:{Type}:{Color ?? "-"}";
    }

    public sealed class OutfitParseResult
    {
        public List<OutfitWish> Wishes { get; } = new List<OutfitWish>();
        /// <summary>Wörter, die nicht verstanden wurden (für einen freundlichen Hinweis in der UI).</summary>
        public List<string> UnknownWords { get; } = new List<string>();

        public OutfitWish Find(OutfitSlot slot) => Wishes.Find(w => w.Slot == slot);
    }

    /// <summary>
    /// Versteht einfache Outfit-Wünsche auf Deutsch und Englisch – offline, ohne KI.
    /// Beispiel: "Blonde Haare, rotes Kleid, braune Schuhe." →
    /// Hair:hair:blonde, Dress:dress:red, Shoes:shoes:brown.
    /// </summary>
    public static class OutfitTextParser
    {
        public const int MaxInputLength = 300;

        private static readonly Dictionary<string, string> Colors = BuildColors();
        private static readonly Dictionary<string, (OutfitSlot slot, string type)> Items = BuildItems();
        private static readonly HashSet<string> FillerWords = new HashSet<string>
        {
            "und", "and", "mit", "with", "ein", "eine", "einen", "einem", "einer", "a", "an", "the",
            "ich", "will", "möchte", "moechte", "hätte", "haette", "gern", "gerne", "bitte", "please",
            "i", "want", "would", "like", "die", "der", "das", "den", "dem", "trägt", "traegt", "wearing", "wears",
            "sie", "er", "she", "he", "figur", "character", "hat", "has", "have", "habe", "soll", "sollen"
        };
        private static readonly HashSet<string> ColorModifiers = new HashSet<string>
        {
            "hell", "helle", "hellen", "heller", "helles", "dunkel", "dunkle", "dunklen", "dunkler", "dunkles", "light", "dark"
        };

        public static OutfitParseResult Parse(string text)
        {
            var result = new OutfitParseResult();
            if (string.IsNullOrWhiteSpace(text)) return result;
            if (text.Length > MaxInputLength) text = text.Substring(0, MaxInputLength);

            foreach (var clause in SplitClauses(Normalize(text)))
            {
                string pendingColor = null;
                foreach (var word in clause.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Colors.TryGetValue(word, out var color))
                    {
                        pendingColor = color;
                    }
                    else if (TryItem(word, out var item))
                    {
                        AddOrReplace(result, new OutfitWish(item.slot, item.type, pendingColor));
                        pendingColor = null;
                    }
                    else if (!FillerWords.Contains(word) && !ColorModifiers.Contains(word))
                    {
                        result.UnknownWords.Add(word);
                    }
                }
            }
            return result;
        }

        private static void AddOrReplace(OutfitParseResult result, OutfitWish wish)
        {
            // Ein Kleid ersetzt Oberteil + Hose und umgekehrt.
            if (wish.Slot == OutfitSlot.Dress)
                result.Wishes.RemoveAll(w => w.Slot == OutfitSlot.Top || w.Slot == OutfitSlot.Bottom);
            if (wish.Slot == OutfitSlot.Top || wish.Slot == OutfitSlot.Bottom)
                result.Wishes.RemoveAll(w => w.Slot == OutfitSlot.Dress);
            if (wish.Slot != OutfitSlot.Accessory)
                result.Wishes.RemoveAll(w => w.Slot == wish.Slot);
            result.Wishes.Add(wish);
        }

        private static bool TryItem(string word, out (OutfitSlot slot, string type) item)
        {
            if (Items.TryGetValue(word, out item)) return true;
            // Einfache Plural-/Endungsregeln: "kleider" → "kleid", "shoes" ist eingetragen, "hüte" → "hut" nicht nötig.
            foreach (var suffix in new[] { "er", "en", "n", "e", "s" })
                if (word.Length > suffix.Length + 2 && word.EndsWith(suffix) && Items.TryGetValue(word.Substring(0, word.Length - suffix.Length), out item))
                    return true;
            return false;
        }

        private static IEnumerable<string> SplitClauses(string text)
        {
            foreach (var part in text.Split(new[] { ',', ';', '.', '!', '?', '\n', '+', '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) yield return trimmed;
            }
        }

        /// <summary>Kleinbuchstaben, Sonderzeichen entfernen; Umlaute bleiben erhalten (beide Schreibweisen sind eingetragen).</summary>
        private static string Normalize(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char ch in text.ToLower(CultureInfo.InvariantCulture))
            {
                if (char.IsLetter(ch) || ch == ' ' || ch == ',' || ch == ';' || ch == '.' || ch == '!' || ch == '?' || ch == '\n' || ch == '+' || ch == '&') sb.Append(ch);
                else if (ch == '-' || ch == '/') sb.Append(' ');
            }
            // "t shirt" (aus "T-Shirt") zusammenführen
            return sb.ToString().Replace("t shirt", "tshirt");
        }

        private static Dictionary<string, string> BuildColors()
        {
            var d = new Dictionary<string, string>();
            void Add(string id, params string[] stems)
            {
                foreach (var stem in stems)
                {
                    d[stem] = id;
                    foreach (var end in new[] { "e", "en", "er", "es", "em" }) d[stem + end] = id;
                }
            }
            Add("red", "rot", "red");
            Add("blue", "blau", "blue");
            Add("green", "grün", "gruen", "green");
            Add("yellow", "gelb", "yellow");
            Add("black", "schwarz", "black");
            Add("white", "weiß", "weiss", "white");
            Add("brown", "braun", "brown", "brünett", "bruenett", "brunette");
            Add("blonde", "blond", "blonde");
            Add("pink", "pink", "rosa", "rosé");
            Add("purple", "lila", "violett", "purple", "violet");
            Add("orange", "orange", "orang");
            Add("grey", "grau", "grey", "gray");
            Add("silver", "silber", "silbern", "silver");
            Add("gold", "gold", "golden");
            Add("beige", "beige", "beig");
            Add("turquoise", "türkis", "tuerkis", "turquoise", "türkies");
            return d;
        }

        private static Dictionary<string, (OutfitSlot, string)> BuildItems()
        {
            var d = new Dictionary<string, (OutfitSlot, string)>();
            void Add(OutfitSlot slot, string type, params string[] words)
            {
                foreach (var w in words) d[w] = (slot, type);
            }
            Add(OutfitSlot.Hair, "hair", "haar", "haare", "hair", "frisur", "hairstyle");
            Add(OutfitSlot.Dress, "dress", "kleid", "dress", "dresses");
            Add(OutfitSlot.Top, "shirt", "shirt", "tshirt", "oberteil", "top", "bluse", "blouse");
            Add(OutfitSlot.Top, "hoodie", "hoodie", "pullover", "pulli", "sweater");
            Add(OutfitSlot.Top, "jacket", "jacke", "jacket");
            Add(OutfitSlot.Bottom, "pants", "hose", "pants", "trousers", "jeans");
            Add(OutfitSlot.Bottom, "skirt", "rock", "skirt");
            Add(OutfitSlot.Bottom, "shorts", "shorts");
            Add(OutfitSlot.Shoes, "shoes", "schuh", "schuhe", "shoe", "shoes");
            Add(OutfitSlot.Shoes, "sneakers", "sneaker", "sneakers", "turnschuh", "turnschuhe");
            Add(OutfitSlot.Shoes, "boots", "stiefel", "boot", "boots");
            Add(OutfitSlot.Accessory, "cap", "mütze", "muetze", "cap", "kappe", "hut", "hat");
            Add(OutfitSlot.Accessory, "glasses", "brille", "glasses", "sonnenbrille", "sunglasses");
            return d;
        }
    }
}
