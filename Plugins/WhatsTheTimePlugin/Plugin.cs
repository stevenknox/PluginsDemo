using System;

namespace PluginSystem
{
    public class Plugin
    {
        public string Execute(string input)
        {
            return $"The time is {DateTime.Now.ToString("hh:mm:ss")} (Request {input})";
        }
    }
}
