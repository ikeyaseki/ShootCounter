namespace ShootCounter.Models
{
    // pistol and revolver manufacturers to choose from in the weapon management,
    // anything else is entered by hand with "Anderer Hersteller"
    public static class Manufacturers
    {
        public const string Other = "Anderer Hersteller …";

        public static IReadOnlyList<string> All { get; } =
        [
            "Arminius",
            "Benelli",
            "Beretta",
            "Bersa",
            "Browning",
            "Canik",
            "Chiappa",
            "Colt",
            "CZ",
            "Dan Wesson",
            "Feinwerkbau",
            "FN Herstal",
            "Glock",
            "Hämmerli",
            "Heckler & Koch",
            "Kimber",
            "Korth",
            "Magnum Research",
            "Manurhin",
            "Morini",
            "Pardini",
            "Rock Island Armory",
            "Ruger",
            "SIG Sauer",
            "Smith & Wesson",
            "Sphinx",
            "Springfield Armory",
            "Steyr",
            "STI / Staccato",
            "Tanfoglio",
            "Taurus",
            "Walther",
            "Wilson Combat",
        ];
    }
}
