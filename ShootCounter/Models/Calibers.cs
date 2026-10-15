namespace ShootCounter.Models
{
    // common calibers to choose from in the weapon management,
    // without brackets because the weapon name shows the caliber in brackets
    public static class Calibers
    {
        public const string None = "Keine Angabe";

        public static IReadOnlyList<string> All { get; } =
        [
            // pistol and revolver
            ".22 lr",
            ".22 WMR",
            ".32 S&W long",
            "7.65 mm Browning",
            "9 mm kurz",
            "9 mm Para",
            "9×21",
            ".38 Special",
            ".357 Magnum",
            ".40 S&W",
            "10 mm Auto",
            ".44 Magnum",
            ".45 ACP",
            "5.7×28",
            "7.62×25 Tokarev",

            // rifle
            ".17 HMR",
            ".223 Remington",
            "5.56×45 GP 90",
            "6.5×55",
            "6.5 Creedmoor",
            "7.5×55 GP 11",
            "7.62×39",
            ".308 Winchester",
            ".30-06 Springfield",
            ".300 Win Mag",

            // air guns
            "4.5 mm Luftdruck",
            "5.5 mm Luftdruck",
        ];
    }
}
