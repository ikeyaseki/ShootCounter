namespace ShootCounter.Models
{
    // pistol and revolver calibers to choose from in the weapon management,
    // without brackets because the weapon name shows the caliber in brackets
    public static class Calibers
    {
        public const string None = "Keine Angabe";

        public static IReadOnlyList<string> All { get; } =
        [
            // rimfire
            ".22 Short",
            ".22 lr",
            ".22 WMR",
            ".17 HMR",

            // small calibers
            "6.35 mm Browning",
            "5.7×28",
            ".30 Super Carry",

            // .32 and 7.5 to 7.65 mm
            ".32 S&W",
            ".32 S&W long",
            ".32 H&R Magnum",
            ".327 Federal Magnum",
            "7.62×25 Tokarev",
            "7.62 Nagant",
            "7.5 mm Ordonnanz 1882",
            "7.65 mm Browning",
            "7.65 mm Parabellum",

            // 9 mm and .38
            "9 mm kurz",
            "9×18 Makarov",
            "9 mm Para",
            "9×21",
            "9×23",
            ".38 S&W",
            ".38 Special",
            ".38 Super",
            ".357 Magnum",
            ".357 SIG",

            // .40 and 10 mm
            ".40 S&W",
            "10 mm Auto",
            ".41 Magnum",

            // .44 and .45
            ".44 Special",
            ".44 Magnum",
            ".45 ACP",
            ".45 GAP",
            ".45 Colt",
            ".454 Casull",
            ".460 S&W Magnum",
            ".480 Ruger",

            // .50
            ".50 AE",
            ".500 S&W Magnum",

            // air pistol
            "4.5 mm Luftpistole",
        ];
    }
}
