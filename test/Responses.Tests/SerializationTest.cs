using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace Responses.Tests
{
    public partial class SerializationTest
    {
        [Fact]
        public void Json_Test1()
        {
            var result = Result.Fail("0001", "Teste");

            var serialized = JsonConvert.SerializeObject(result);

            var back = JsonConvert.DeserializeObject<Result>(serialized);

            Assert.Equal(result.Error.ApplicationName, back.Error.ApplicationName);
            Assert.Equal(result.Error.Code, back.Error.Code);
            Assert.Equal(result.Error.Layer, back.Error.Layer);
            Assert.Equal(result.Error.Message, back.Error.Message);
        }

        [Fact]
        public void Json_Test2()
        {
            var result = Result.Ok(1);

            var serialized = JsonConvert.SerializeObject(result);

            var back = JsonConvert.DeserializeObject<Result<int>>(serialized);

            Assert.Equal(result.Value, back.Value);
        }

        [Fact]
        public void Json_Test3()
        {
            var result = Result.Fail<int, TestError>(new TestError
            {
                Code = "007",
                ApplicationName = "Bond",
                Layer = "Client",
                Message = "James Bond"
            });

            var serialized = JsonConvert.SerializeObject(result);

            var back = JsonConvert.DeserializeObject<Result<int, TestError>>(serialized);

            Assert.Throws<InvalidOperationException>(() => back.Value);
            Assert.Equal(result.Error.ApplicationName, back.Error.ApplicationName);
            Assert.Equal(result.Error.Code, back.Error.Code);
            Assert.Equal(result.Error.Layer, back.Error.Layer);
            Assert.Equal(result.Error.Message, back.Error.Message);
        }

        [Fact]
        public void Json_Test4()
        {
            var result = Result.Fail<int, Error>(new Error
            (
                "007",
                "James Bond",
                new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("001","O Coisa"),
                    new KeyValuePair<string, string>("002","Tocha-Humana" ),
                    new KeyValuePair<string, string>("003","Dr. Fantastico" ),
                    new KeyValuePair<string, string>("004","Mulher Invisivel" )
                }
            ));

            var serialized = JsonConvert.SerializeObject(result);

            var back = JsonConvert.DeserializeObject<Result<int, Error>>(serialized);

            Assert.Throws<InvalidOperationException>(() => back.Value);
            Assert.Equal(result.Error.ApplicationName, back.Error.ApplicationName);
            Assert.Equal(result.Error.Code, back.Error.Code);
            Assert.Equal(result.Error.Layer, back.Error.Layer);
            Assert.Equal(result.Error.Message, back.Error.Message);
            Assert.Equal(result.Error.Errors, back.Error.Errors);
        }

        [Fact]
        public void Json_Test5()
        {
            var result = Result.Fail<int>("0001", "Teste");

            var serialized = JsonConvert.SerializeObject(result);

            var back = JsonConvert.DeserializeObject<Result>(serialized);

            Assert.Equal(result.Error.ApplicationName, back.Error.ApplicationName);
            Assert.Equal(result.Error.Code, back.Error.Code);
            Assert.Equal(result.Error.Layer, back.Error.Layer);
            Assert.Equal(result.Error.Message, back.Error.Message);
        }

        [Fact(DisplayName = "Quando deserializar um 'Result' de 'Error', dado que objeto est� serializado com 'IsSuccess' igual a 'true', ent�o desve desserializar objeto com sucesso")]
        [Trait("Deserialize", "AggregatedErrorResult")]
        public void WhenDeserializeErrorResult_GivenObjecIsSerializedWithIsSuccessTrue_ThenDeserialeObjectWithSuccess()
        {
            var obj = Result.Ok(1);

            var serialized = JsonConvert.SerializeObject(obj);

            var back = JsonConvert.DeserializeObject<Result<int>>(serialized);

            Assert.True(back.IsSuccess);
            Assert.Throws<InvalidOperationException>(() => back.Error);
            Assert.Equal(1, back.Value);
        }

        [Fact(DisplayName = "Quando deserializar um 'Result' de 'Error', dado que objeto est� serializado com 'IsSuccess' igual a 'true', ent�o desve desserializar objeto com sucesso")]
        [Trait("Deserialize", "AggregatedErrorResult")]
        public void WhenDeserializeErrorResult_GivenObjecIsSerializedWithIsSuccessFalse_ThenDeserialeObjectWithSuccess()
        {
            var code = "01";
            var message = "teste";
            var obj = Result.Fail<int>(code, message);

            var serialized = JsonConvert.SerializeObject(obj);

            var back = JsonConvert.DeserializeObject<Result<int>>(serialized);

            Assert.False(back.IsSuccess);
            Assert.Throws<InvalidOperationException>(() => back.Value);
            Assert.Equal(code, back.Error.Code);
            Assert.Equal(message, back.Error.Message);
        }
    }
}