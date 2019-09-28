using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WebApp.PluginSystem
{
    public class PluginService
    {
        private Assembly SystemRuntime = Assembly.Load(new AssemblyName("System.Runtime"));
        public Dictionary<string, List<string>> PluginResponses { get; private set; } = new Dictionary<string, List<string>>();
        public List<HostedPlugin> Plugins { get; set; } = new List<HostedPlugin>();

        public string DefaultCode = @"public class Plugin
        {
            public string Execute(string input)
            {
               //return a string here
            }
        }";

        public void LoadPlugins()
        {
            var assembliesPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            foreach (var pluginfolder in Directory.EnumerateDirectories(assembliesPath))
            {
                Plugins.Add(new HostedPlugin {
                    Name = Path.GetFileName(pluginfolder),
                    FilePath = Path.Combine(pluginfolder, $"{Path.GetFileName(pluginfolder)}.dll"),
                });
            }
        }

        // put entire UnloadableAssemblyLoadContext in a method to avoid caller
        // holding on to the reference
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteAssembly(HostedPlugin plugin, string input)
        {
            var context = new CollectibleAssemblieContext();
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
        private void ExecuteInMemoryAssembly(Compilation compilation, string input)
        {
            var context = new CollectibleAssemblieContext();

            using (var ms = new MemoryStream())
            {
                var cr = compilation.Emit(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = context.LoadFromStream(ms);

                var type = assembly.GetType("Plugin");
                var executeMethod = type.GetMethod("Execute");

                var instance = Activator.CreateInstance(type);

                var dic = PluginResponses.GetOrCreate("DynamicPlugin");

                dic.Add(executeMethod.Invoke(instance, new object[] { input }).ToString());
            }

            context.Unload();
        }

        public void RunPlugin(HostedPlugin plugin, string input)
        {
           
            ExecuteAssembly(plugin, input);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public void RunDynamicPlugin(string syntax, string input)
        {
           
            var compilation = CSharpCompilation.Create("DynamicAssembly", new[] { CSharpSyntaxTree.ParseText(syntax) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(SystemRuntime.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

             ExecuteInMemoryAssembly(compilation, input);
           

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


    }
}