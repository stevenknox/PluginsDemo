using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;

namespace PluginSystem
{
    public class Plugin
    {
        private readonly HttpClient client = new HttpClient();
        public string Execute(string input)
        {
        
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", "GitHub Repository Reporter");

            var serializer = new DataContractJsonSerializer(typeof(List<repo>));
            var streamTask = client.GetStreamAsync("https://api.github.com/users/stevenknox/repos").Result;
            
            var repositories = serializer.ReadObject(streamTask) as List<repo>;
            
            return string.Join(", ", repositories.Where(f=> f.fork == false)
                                                .Take(15)
                                                .OrderByDescending(o=> o.stargazers_count)
                                                .Select(s => s.name));
        }
    }
     public class repo
    {
        public string name;
        public int stargazers_count;
        public bool fork { get; set; }
    }
}
