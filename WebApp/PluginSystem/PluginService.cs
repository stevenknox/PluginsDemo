using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WebApp.PluginSystem
{
    public class PluginService
    {
        private Assembly SystemRuntime = Assembly.Load(new AssemblyName("System.Runtime"));
        public Dictionary<string, List<string>> PluginResponses { get; private set; } = new Dictionary<string, List<string>>();
        public List<HostedPlugin> Plugins { get; set; } = new List<HostedPlugin>();

        public void LoadPlugins()
        {
            var assembliesPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            foreach (var pluginfolder in Directory.EnumerateDirectories(assembliesPath))
            {
                var pluginName = Path.GetFileName(pluginfolder);
                if (Plugins.FirstOrDefault(f => f.Name == pluginName) == null)
                {
                    Plugins.Add(new HostedPlugin
                    {
                        Name = pluginName,
                        FilePath = Path.Combine(pluginfolder, $"{pluginName}.dll"),
                    });
                }
            }
        }

        // put entire UnloadableAssemblyLoadContext in a method to avoid caller
        // holding on to the reference
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteAssembly(HostedPlugin plugin, string input)
        {
            var context = new CollectibleAssemblyContext();
            var assemblyPath = Path.Combine(plugin.FilePath);
            using (var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
            {
                var assembly = context.LoadFromStream(fs);

                var type = assembly.GetType("PluginSystem.Plugin");
                var executeMethod = type.GetMethod("Execute");

                var instance = Activator.CreateInstance(type);

                var dic = PluginResponses.GetOrCreate(plugin.Name);

                dic.Add(executeMethod.Invoke(instance, new object[] { input }).ToString());
            }

            context.Unload();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteInMemoryAssembly(HostedPlugin plugin, string input)
        {
            var context = new CollectibleAssemblyContext();

            using (var ms = new MemoryStream())
            {
                var compilation = CSharpCompilation.Create(plugin.Name, new[] { CSharpSyntaxTree.ParseText(plugin.Code) },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(SystemRuntime.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                var cr = compilation.Emit(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = context.LoadFromStream(ms);

                var type = assembly.GetType("Plugin");
                var executeMethod = type.GetMethod("Execute");

                var instance = Activator.CreateInstance(type);

                var dic = PluginResponses.GetOrCreate(plugin.Name);

                dic.Add(executeMethod.Invoke(instance, new object[] { input }).ToString());
            }

            context.Unload();
        }

        public void RunPlugin(HostedPlugin plugin, string input)
        {
            if (plugin.InMemory)
                RunDynamicPlugin(plugin, input);
            else
                ExecuteAssembly(plugin, input);

            RunGarbageCollection();
        }

        public void CreateOrRunDynamicPlugin(string syntax, string input)
        {
            //if hosted assembly, just execute, else create
            var name = $"DynamicPlugin{input}";
            var plugin = Plugins.FirstOrDefault(f => f.Name == name);
            if (plugin == null)
            {

                plugin = new HostedPlugin();
                plugin.Name = name;
                plugin.Code = syntax;

            }

            Plugins.Add(plugin);

            ExecuteInMemoryAssembly(plugin, input);

            RunGarbageCollection();
        }
        public void RunDynamicPlugin(HostedPlugin plugin, string input)
        {
            ExecuteInMemoryAssembly(plugin, input);

            RunGarbageCollection();
        }


        private static void RunGarbageCollection()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (System.Exception)
            {
                //sometimes GC.Collet/WaitForPendingFinalizers crashes, just ignore for this blog post
            }
        }

    }
}