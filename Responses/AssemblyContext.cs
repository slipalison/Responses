using System;
using System.Reflection;

namespace Responses
{
    internal static class AssemblyContext
    {
        internal static Func<string> GetAssemblyName =
            () => Assembly.GetEntryAssembly().GetName().Name;
    }
}