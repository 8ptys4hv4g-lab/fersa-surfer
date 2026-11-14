using System;
using System.Collections.Generic;

namespace WorldRush.Core.Outfits
{
    /// <summary>Ein Kleidungsstück im lokalen 3D-Katalog (das 3D-Modell kommt in M6).</summary>
    public sealed class OutfitItem
    {
        public OutfitItem(string id, OutfitSlot slot, string type, params string[] colors)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Slot = slot;
            Type = type;
            Colors = colors ?? Array.Empty<string>();
        }

        public string Id { get; }
        public OutfitSlot Slot { get; }
        public string Type { get; }
        public string[] Colors { get; }

        public bool HasColor(string color) => Array.IndexOf(Colors, color) >= 0;
    }

    public enum MatchQuality
    {
        /// <summary>Genau wie gewünscht.</summary>
        Exact,
        /// <summary>Teil gefunden, Farbe ersetzt.</summary>
        ColorSubstituted,
        /// <summary>Ähnliches Teil im selben Bereich (z. B. Sneaker statt Stiefel).</summary>
        ItemSubstituted,
        /// <summary>Nichts Passendes vorhanden.</summary>
        NotAvailable
    }

    public sealed class OutfitChoice
    {
        public OutfitChoice(OutfitWish wish, OutfitItem item, string color, MatchQuality quality)
        {
            Wish = wish;
            Item = item;
            Color = color;
            Quality = quality;
        }

        public OutfitWish Wish { get; }
        public OutfitItem Item { get; }
        public string Color { get; }
        public MatchQuality Quality { get; }
    }

    /// <summary>
    /// Sucht zu jedem Wunsch das beste Teil im lokalen Katalog.
    /// Erzeugt NIE etwas Neues und behauptet das auch nicht: Ersatz wird als solcher markiert.
    /// </summary>
    public sealed class OutfitCatalog
    {
        private readonly List<OutfitItem> _items = new List<OutfitItem>();

        public OutfitCatalog(IEnumerable<OutfitItem> items)
        {
            var ids = new HashSet<string>();
            foreach (var item in items)
            {
                if (!ids.Add(item.Id)) throw new ArgumentException("Doppelte Outfit-ID: " + item.Id);
                _items.Add(item);
            }
        }

        public IReadOnlyList<OutfitItem> Items => _items;

        /// <summary>Kleiner Start-Katalog für Prototyp und Tests.</summary>
        public static OutfitCatalog CreateStarter() => new OutfitCatalog(new[]
        {
            new OutfitItem("hair-medium", OutfitSlot.Hair, "hair", "blonde", "brown", "black", "red", "grey", "pink", "blue"),
            new OutfitItem("dress-classic", OutfitSlot.Dress, "dress", "red", "blue", "black", "white", "green", "yellow", "pink", "purple"),
            new OutfitItem("shirt-basic", OutfitSlot.Top, "shirt", "white", "black", "red", "blue", "green", "yellow", "grey"),
            new OutfitItem("hoodie-street", OutfitSlot.Top, "hoodie", "black", "grey", "blue", "purple", "orange"),
            new OutfitItem("pants-jeans", OutfitSlot.Bottom, "pants", "blue", "black", "grey", "beige"),
            new OutfitItem("skirt-short", OutfitSlot.Bottom, "skirt", "black", "red", "blue", "pink"),
            new OutfitItem("shoes-classic", OutfitSlot.Shoes, "shoes", "brown", "black", "white", "red"),
            new OutfitItem("sneakers-run", OutfitSlot.Shoes, "sneakers", "white", "black", "red", "blue", "green", "orange"),
            new OutfitItem("cap-sport", OutfitSlot.Accessory, "cap", "red", "blue", "black", "white"),
        });

        public List<OutfitChoice> Match(OutfitParseResult request)
        {
            var choices = new List<OutfitChoice>();
            foreach (var wish in request.Wishes) choices.Add(Match(wish));
            return choices;
        }

        public OutfitChoice Match(OutfitWish wish)
        {
            OutfitItem sameType = _items.Find(i => i.Slot == wish.Slot && i.Type == wish.Type);
            if (sameType != null)
            {
                if (wish.Color == null) return new OutfitChoice(wish, sameType, sameType.Colors.Length > 0 ? sameType.Colors[0] : null, MatchQuality.Exact);
                if (sameType.HasColor(wish.Color)) return new OutfitChoice(wish, sameType, wish.Color, MatchQuality.Exact);
            }

            // Anderes Teil im selben Bereich, das die Wunschfarbe hat.
            if (wish.Color != null)
            {
                var other = _items.Find(i => i.Slot == wish.Slot && i.HasColor(wish.Color));
                if (other != null) return new OutfitChoice(wish, other, wish.Color, MatchQuality.ItemSubstituted);
            }

            if (sameType != null)
                return new OutfitChoice(wish, sameType, sameType.Colors.Length > 0 ? sameType.Colors[0] : null, MatchQuality.ColorSubstituted);

            var anyInSlot = _items.Find(i => i.Slot == wish.Slot);
            if (anyInSlot != null)
                return new OutfitChoice(wish, anyInSlot, anyInSlot.Colors.Length > 0 ? anyInSlot.Colors[0] : null, MatchQuality.ItemSubstituted);

            return new OutfitChoice(wish, null, null, MatchQuality.NotAvailable);
        }
    }
}
