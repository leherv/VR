namespace Persistence
{
    public class PersistenceAssemblyMarker
    {
        public static string GetAssemblyName => typeof(PersistenceAssemblyMarker).Assembly.GetName().Name;
    }
}