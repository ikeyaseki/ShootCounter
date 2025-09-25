namespace ShootCounter.Services
{
    public static class ServiceHelper
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public static IServiceProvider Services { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public static void Initialize(IServiceProvider serviceProvider) => 
            Services = serviceProvider;

#pragma warning disable CS8603 // Possible null reference return.
        public static T GetService<T>() => Services.GetService<T>();
#pragma warning restore CS8603 // Possible null reference return.
    }
}
