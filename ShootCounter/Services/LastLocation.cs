namespace ShootCounter.Services
{
    // the location of the last training or competition, proposed for the next one:
    // the app is mostly used at the same shooting range
    public static class LastLocation
    {
        private const string Key = "LastLocation";

        public static string Value
        {
            get => Preferences.Default.Get(Key, string.Empty);
            set => Preferences.Default.Set(Key, value.Trim());
        }
    }
}
