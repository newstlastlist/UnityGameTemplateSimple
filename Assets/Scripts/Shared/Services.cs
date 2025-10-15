namespace Shared
{
    public static class Services
    {
        private static IServiceRegistry _provider;

        public static void SetProvider(IServiceRegistry provider)
        {
            _provider = provider;
        }

        public static void Register<T>(T service) where T : class
        {
            _provider.Register(service);
        }

        public static T Get<T>() where T : class
        {
            return _provider.Resolve<T>();
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            return _provider.TryResolve(out service);
        }
    }
}