using System.Linq;
using System.Text.RegularExpressions;

namespace Responses
{
    internal static class ResultContext
    {
        private static (string Layer, string ApplicationName) config = GetConfiguration();
        public static readonly string Layer = config.Layer;
        public static readonly string ApplicationName = config.ApplicationName;

        public static (string Layer, string ApplicationName) GetConfiguration()
        {
            var assemblyName = AssemblyContext.GetAssemblyName();

            var layer = assemblyName.Split('.').FirstOrDefault();

            return (layer, GetApplicationName(assemblyName));
        }

        private static string GetApplicationName(string applicationName)
        {
            var upperLetters = Regex.Matches(applicationName, "[A-Z]");

            int lastUpperLetter = -1;

            string result = string.Join(string.Empty, upperLetters.Cast<Match>().Take(4).Select(x => x.Value));

            if (upperLetters.Count >= 4) return result;
            else if (upperLetters.Count > 0)
                lastUpperLetter = applicationName.IndexOf(upperLetters[upperLetters.Count - 1].Value);

            if (lastUpperLetter >= -1)
                return (result + string.Join(string.Empty, applicationName.Skip(lastUpperLetter + 1).Take(4 - upperLetters.Count))).ToUpper();

            return null;
        }
    }
}