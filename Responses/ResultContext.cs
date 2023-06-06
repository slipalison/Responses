using System.Linq;

namespace Responses;

internal static class ResultContext
{
    private static (string Layer, string ApplicationName) config = GetConfiguration();
    public static readonly string Layer = config.Layer;
    public static readonly string ApplicationName = config.ApplicationName;

    public static (string Layer, string ApplicationName) GetConfiguration()
    {
        var assemblyName = AssemblyContext.GetAssemblyName();

        var layer = assemblyName.Split('.').FirstOrDefault();

        return (layer, assemblyName);
    }

}
