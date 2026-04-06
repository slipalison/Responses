using System.Reflection;

namespace Responses;

/// <summary>
/// Provides immutable assembly context information for error metadata.
/// </summary>
internal static class AssemblyContext
{
    private static readonly string _assemblyName = InitializeAssemblyName();

    private static string InitializeAssemblyName()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the assembly name safely without mutation.
    /// </summary>
    internal static string GetAssemblyName() => _assemblyName;
}
