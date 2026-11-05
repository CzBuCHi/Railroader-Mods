using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System;

namespace Tweaks.Tests.Infrastructure
{
    internal static class CompiledMod
    {
        private static string? managedDirectory;

        internal static Assembly Load(string managed, string modPath)
        {
            if (managedDirectory == null)
            {
                managedDirectory = Path.GetFullPath(managed);
                AssemblyLoadContext.Default.Resolving += (context, name) =>
                {
                    var path = Path.Combine(managedDirectory, name.Name + ".dll");
                    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
                };
            }
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(modPath));
        }

        internal static T Invoke<T>(MethodInfo method, params object[] args)
        {
            try { return (T)method.Invoke(null, args)!; }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
