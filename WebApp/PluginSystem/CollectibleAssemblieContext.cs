using System.Reflection;
using System.Runtime.Loader;

namespace WebApp.PluginSystem
{
    public class CollectibleAssemblieContext : AssemblyLoadContext
    {
        public CollectibleAssemblieContext() : base(isCollectible: true)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}