namespace WorldRush.Core
{
    /// <summary>
    /// Sichtbarer Produktname. Interner Code-Name bleibt "WorldRush" (Ordner, Namespaces),
    /// damit eine spätere Umbenennung nur hier nötig ist.
    /// </summary>
    public static class ProductInfo
    {
        public const string DisplayName = "Fesara";
        /// <summary>Untertitel unter dem Logo und im Store-Namen ("Fesara – Run & Surf").</summary>
        public const string Tagline = "Run & Surf";
        public const string StoreTitle = DisplayName + " \u2013 " + Tagline;
        public const string Version = "0.4.2";
        // Bleibt absichtlich gleich, damit vorhandene Spielstände erhalten bleiben.
        public const string SaveFileName = "fersasurfer-save.json";
    }
}
