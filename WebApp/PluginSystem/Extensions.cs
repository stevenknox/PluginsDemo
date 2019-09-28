using System.Collections.Generic;

namespace WebApp.PluginSystem
{
    public static class Extensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) 
            where TValue : new()
        {
            TValue val;

            if (!dict.TryGetValue(key, out val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }
    }
}