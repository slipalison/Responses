using System.Collections.Generic;

namespace Responses.Tests;

public partial class SerializationTest
{
    public class TestError : IError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ErrorType Type { get; set; }
        public string Layer { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
