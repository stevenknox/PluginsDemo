namespace WebApp.PluginSystem
{
    public class HostedPlugin
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public bool InMemory { get; set; }
        public string Code { get; set; }
    }
}