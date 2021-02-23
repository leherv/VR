using System.Reflection;

namespace VRNotifier
{
    public class NotifierAssemblyMarker
    {
        public static string GetAssemblyName => typeof(NotifierAssemblyMarker).Assembly.GetName().Name;
        public static Assembly GetAssembly => typeof(NotifierAssemblyMarker).Assembly;

    }
}