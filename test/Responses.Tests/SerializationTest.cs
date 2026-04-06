using System;
using System.Collections.Generic;
using Xunit;

namespace Responses.Tests;

public partial class SerializationTest
{
    private const string SkipReason = "Phase 2: STJ serialization migration - Newtonsoft.Json removed";

    [Fact(Skip = SkipReason)]
    public void Json_Test1()
    {
        var result = Result.Fail("0001", "Teste");
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(serialized);
        Assert.Equal(result.Error.ApplicationName, back!.Error.ApplicationName);
    }

    [Fact(Skip = SkipReason)]
    public void Json_Test2()
    {
        var result = Result.Ok(1);
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result<int>>(serialized);
        Assert.Equal(result.Value, back!.Value);
    }

    [Fact(Skip = SkipReason)]
    public void Json_Test3()
    {
        var result = Result.Fail<int, TestError>(new TestError { Code = "007", ApplicationName = "Bond", Layer = "Client", Message = "James Bond" });
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result<int, TestError>>(serialized);
        Assert.Throws<InvalidOperationException>(() => back!.Value);
    }

    [Fact(Skip = SkipReason)]
    public void Json_Test4()
    {
        var result = Result.Fail<int, Error>(new Error("007", "James Bond"));
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result<int, Error>>(serialized);
        Assert.Equal(result.Error.Code, back!.Error.Code);
    }

    [Fact(Skip = SkipReason)]
    public void Json_Test5()
    {
        var result = Result.Fail<int>("0001", "Teste");
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(serialized);
        Assert.Equal(result.Error.Code, back!.Error.Code);
    }

    [Fact(Skip = SkipReason)]
    public void WhenDeserializeErrorResult_GivenObjecIsSerializedWithIsSuccessTrue_ThenDeserialeObjectWithSuccess()
    {
        var obj = Result.Ok(1);
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result<int>>(serialized);
        Assert.True(back!.IsSuccess);
        Assert.Equal(1, back.Value);
    }

    [Fact(Skip = SkipReason)]
    public void WhenDeserializeErrorResult_GivenObjecIsSerializedWithIsSuccessFalse_ThenDeserialeObjectWithSuccess()
    {
        var obj = Result.Fail<int>("01", "teste");
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<Result<int>>(serialized);
        Assert.False(back!.IsSuccess);
        Assert.Equal("01", back.Error.Code);
    }
}
