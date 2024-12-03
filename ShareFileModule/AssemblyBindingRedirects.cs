using System;
using System.Collections.Generic;
using System.Reflection;

namespace ShareFile.Api.Powershell
{
    internal static class AssemblyBindingRedirects
    {
        private static Lazy<bool> lazyEventHandlerInitializer;

        /// <summary>
        /// Replacement for assemblyBinding elements in App.config file.
        /// The key is the simple name of the assembly.
        /// The value is the fully qualified name of the assembly to return.
        /// </summary>
        private static readonly Dictionary<string, string> bindingRedirects = new Dictionary<string, string>
        {
            { "System.Buffers", "System.Buffers, Version=4.0.2.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51" },
            { "Newtonsoft.Json", "Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed" },
            { "System.Net.Http.Primitives", "System.Net.Http.Primitives, Version=4.2.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" }
        };

        static AssemblyBindingRedirects()
        {
            // Make use of Lazy<T> to make sure the event registration happens only once, and is thread safe.
            // The bool is just because we have to return something.
            lazyEventHandlerInitializer = new Lazy<bool>(() =>
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
                return true;
            });
        }

        /// <summary>
        /// Register assembly binding redirects if they haven't been already.
        /// Additional calls to this method have no effect.
        /// </summary>
        public static void Register()
        {
            _ = lazyEventHandlerInitializer.Value;
        }

        /// <summary>
        /// Normally, binding redirects are defined in the .config file for the assembly. However, these get ignore when
        /// we get loaded in PowerShell as a module.
        /// So we have to do this manually using the AppDomain.CurrentDomain.AssemblyResolve callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">The assembly being requested and the assembly that requires it as a dependency</param>
        /// <returns>Returns the assembly for the given assembly name, or null if it's not known.</returns>
        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            if (bindingRedirects.TryGetValue(assemblyName.Name, out var fullName))
            {
                return Assembly.Load(fullName);
            }
            return null;
        }
    }
}
