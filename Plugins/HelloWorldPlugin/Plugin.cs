using System;

namespace PluginSystem
{
    public class Plugin
    {
        public string Execute(string input)
        {
            return $"Hello World! (Request {input})";
        }
    }
}
