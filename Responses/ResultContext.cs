using System.Linq;

namespace Responses;

internal static class ResultContext
{
    private static readonly (string Layer, string ApplicationName) _config = GetConfiguration();
    public static readonly string Layer = _config.Layer;
    public static readonly string ApplicationName = _config.ApplicationName;

    private static (string Layer, string ApplicationName) GetConfiguration()
    {
        var assemblyName = AssemblyContext.GetAssemblyName();
        var layer = assemblyName.Split('.').FirstOrDefault() ?? assemblyName;
        return (layer, assemblyName);
    }
}
