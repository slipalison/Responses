using System;
using System.Runtime.Serialization;

namespace Responses.Tests
{
    public partial class SerializationTest
    {
        [Serializable]
        public class TestError : IError
        {
            public string Code { get; set; }

            public string Message { get; set; }

            public string Layer { get; set; }

            public string ApplicationName { get; set; }
        }
    }
}