namespace WorldRush.Core
{
    /// <summary>Ein Surfbrett zum Freischalten – rein kosmetisch, kein Spielvorteil.</summary>
    public sealed class BoardItem
    {
        public BoardItem(int id, string name, int price, string colorHex)
        {
            Id = id;
            Name = name;
            Price = price;
            ColorHex = colorHex;
        }

        public int Id { get; }
        public string Name { get; }
        public int Price { get; }
        public string ColorHex { get; }
    }

    /// <summary>
    /// Surfbrett-Shop mit Spiel-Münzen (kein Echtgeld). Besitz als Bitmaske im Spielstand.
    /// </summary>
    public static class BoardShop
    {
        public static readonly BoardItem[] Items =
        {
            new BoardItem(0, "Neon-Türkis", 0, "#3de8ff"),
            new BoardItem(1, "Sonnen-Gelb", 150, "#ffd23f"),
            new BoardItem(2, "Pink-Blitz", 300, "#ff4fa3"),
            new BoardItem(3, "Galaxie-Lila", 500, "#8f5bff"),
            new BoardItem(4, "Minz-Welle", 750, "#5ff2b0"),
            new BoardItem(5, "Regenbogen-Gold", 1000, "#e3b23c"),
        };

        public static BoardItem Get(int id) => id >= 0 && id < Items.Length ? Items[id] : Items[0];

        public static bool Owns(SaveData save, int id) => id == 0 || (id > 0 && id < 31 && (save.OwnedBoards & (1 << id)) != 0);

        public enum BuyResult { Bought, AlreadyOwned, NotEnoughCoins, Unknown }

        public static BuyResult TryBuy(SaveData save, int id)
        {
            if (id < 0 || id >= Items.Length) return BuyResult.Unknown;
            if (Owns(save, id)) return BuyResult.AlreadyOwned;
            var item = Items[id];
            if (save.Wallet < item.Price) return BuyResult.NotEnoughCoins;
            save.Wallet -= item.Price;
            save.OwnedBoards |= 1 << id;
            save.SelectedBoard = id;
            return BuyResult.Bought;
        }

        public static bool Select(SaveData save, int id)
        {
            if (!Owns(save, id) || id >= Items.Length) return false;
            save.SelectedBoard = id;
            return true;
        }
    }
}
