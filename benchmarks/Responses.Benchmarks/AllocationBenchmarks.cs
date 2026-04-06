using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Responses.Benchmarks;

/// <summary>
/// Benchmarks to verify zero-allocation claims for Result types.
/// </summary>
public class ResultAllocationBenchmarks
{
    private static readonly Error _error = Error.Validation("ERR", "message");
    private static readonly Func<int, int> _mapFunc = x => x * 2;
    private static readonly Func<int, Result<int>> _bindFunc = x => Result.Ok(x * 2);

    [Benchmark]
    public Result Ok() => Result.Ok();

    [Benchmark]
    public Result<int> OkWithValue() => Result.Ok(42);

    [Benchmark]
    public Result Fail() => Result.Fail("ERR", "msg");

    [Benchmark]
    public Result<int> FailWithValue() => Result.Fail<int>("ERR", "msg");

    [Benchmark]
    public Result<int> Map_Success() => Result.Ok(5).Map(_mapFunc);

    [Benchmark]
    public Result<int> Map_Failure() => Result.Fail<int>("ERR", "msg").Map(_mapFunc);

    [Benchmark]
    public Result<int> Bind_Success() => Result.Ok(5).Bind(_bindFunc);

    [Benchmark]
    public Result<int> Bind_Failure() => Result.Fail<int>("ERR", "msg").Bind(_bindFunc);

    [Benchmark]
    public int ValueOrDefault_Success() => Result.Ok(42).ValueOrDefault;

    [Benchmark]
    public int ValueOrDefault_Failure() => Result.Fail<int>("ERR", "msg").ValueOrDefault;
}

/// <summary>
/// Benchmarks for Error type allocation.
/// </summary>
public class ErrorAllocationBenchmarks
{
    [Benchmark]
    public Error CreateError() => new("ERR", "message");

    [Benchmark]
    public Error CreateErrorWithType() => Error.Validation("ERR", "message");

    [Benchmark]
    public Error CreateErrorWithMetadata() => new Error("ERR", "message", ErrorType.Validation, new Dictionary<string, string> { { "key", "value" } });
}

public static class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ResultAllocationBenchmarks>();
        Console.WriteLine(summary);

        var errorSummary = BenchmarkRunner.Run<ErrorAllocationBenchmarks>();
        Console.WriteLine(errorSummary);
    }
}
