using Xunit;
using static Responses.Tests.SerializationTest;

namespace Responses.Tests;

/// <summary>
/// Tests for LINQ query syntax support via SelectMany.
/// Covers Result&lt;T&gt; and Result&lt;TValue, TError&gt;.
/// </summary>
public class LinqSupportTests
{
    #region SelectMany Tests for Result&lt;T&gt;

    public class SelectManyOfTTests
    {
        [Fact]
        public void SelectMany_ChainsMultipleResults_WhenAllSuccess()
        {
            var query = from x in Result.Ok(5)
                        from y in Result.Ok(10)
                        select x + y;
            Assert.True(query.IsSuccess);
            Assert.Equal(15, query.Value);
        }

        [Fact]
        public void SelectMany_ShortCircuits_WhenFirstFails()
        {
            var callCount = 0;
            var query = from x in Result.Fail<int>("ERR", "msg")
                        from y in Result.Ok(10).Tap(_ => callCount++)
                        select x + y;

            Assert.True(query.IsFailed);
            Assert.Equal(0, callCount); // second operation was NOT called
        }

        [Fact]
        public void SelectMany_ShortCircuits_WhenSecondFails()
        {
            var query = from x in Result.Ok(5)
                        from y in Result.Fail<int>("STOP", "stop")
                        select x + y;

            Assert.True(query.IsFailed);
            Assert.Equal("STOP", query.Error.Code);
        }

        [Fact]
        public void SelectMany_CombinesThreeResults()
        {
            var query = from a in Result.Ok(1)
                        from b in Result.Ok(2)
                        from c in Result.Ok(3)
                        select a + b + c;
            Assert.True(query.IsSuccess);
            Assert.Equal(6, query.Value);
        }

        [Fact]
        public void SelectMany_TransformsTypes()
        {
            var query = from num in Result.Ok(42)
                        from str in Result.Ok($"value: {num}")
                        select str.Length;
            Assert.True(query.IsSuccess);
            Assert.Equal(9, query.Value); // "value: 42" has 9 chars
        }

        [Fact]
        public void SelectMany_PropagatesFirstError()
        {
            var query = from a in Result.Ok(1)
                        from b in Result.Fail<int>("FIRST", "first error")
                        from c in Result.Fail<int>("SECOND", "second error")
                        select a + b + c;

            Assert.True(query.IsFailed);
            Assert.Equal("FIRST", query.Error.Code);
        }

        [Fact]
        public void SelectMany_WithIntermediateBinding()
        {
            var query = from x in Result.Ok(3)
                        from y in Result.Ok(4)
                        let sum = x + y
                        from z in Result.Ok(sum * 2)
                        select z;

            Assert.True(query.IsSuccess);
            Assert.Equal(14, query.Value); // (3+4)*2 = 14
        }
    }

    #endregion

    #region SelectMany Tests for Result&lt;TValue, TError&gt;

    public class SelectManyOfTValueTErrorTests
    {
        [Fact]
        public void SelectMany_ChainsMultipleResults_WhenAllSuccess()
        {
            var query = from x in Result.Ok<int, TestError>(5)
                        from y in Result.Ok<int, TestError>(10)
                        select x + y;
            Assert.True(query.IsSuccess);
            Assert.Equal(15, query.Value);
        }

        [Fact]
        public void SelectMany_ShortCircuits_WhenFirstFails()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var callCount = 0;
            var query = from x in Result.Fail<int, TestError>(error)
                        from y in Result.Ok<int, TestError>(10).Tap(_ => callCount++)
                        select x + y;

            Assert.True(query.IsFailed);
            Assert.Equal(0, callCount);
            Assert.Equal("TERR", query.Error.Code);
        }

        [Fact]
        public void SelectMany_ShortCircuits_WhenSecondFails()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var query = from x in Result.Ok<int, TestError>(5)
                        from y in Result.Fail<int, TestError>(error)
                        select x + y;

            Assert.True(query.IsFailed);
            Assert.Equal("TERR", query.Error.Code);
        }

        [Fact]
        public void SelectMany_PreservesTypedError()
        {
            var query = from x in Result.Ok<int, TestError>(3)
                        from y in Result.Ok<int, TestError>(4)
                        select x * y;

            Assert.True(query.IsSuccess);
            Assert.Equal(12, query.Value);
        }

        [Fact]
        public void SelectMany_CombinesThreeResults_WithTypedError()
        {
            var query = from a in Result.Ok<int, TestError>(1)
                        from b in Result.Ok<int, TestError>(2)
                        from c in Result.Ok<int, TestError>(3)
                        select a + b + c;
            Assert.True(query.IsSuccess);
            Assert.Equal(6, query.Value);
        }
    }

    #endregion

    #region Query Syntax Edge Cases

    public class QuerySyntaxEdgeCases
    {
        [Fact]
        public void QuerySyntax_SingleResult()
        {
            var query = from x in Result.Ok(5)
                        select x * 2;
            Assert.True(query.IsSuccess);
            Assert.Equal(10, query.Value);
        }

        [Fact]
        public void QuerySyntax_SingleFailedResult()
        {
            var query = from x in Result.Fail<int>("ERR", "msg")
                        select x * 2;
            Assert.True(query.IsFailed);
            Assert.Equal("ERR", query.Error.Code);
        }

        [Fact]
        public void QuerySyntax_WithStringTypes()
        {
            var query = from firstName in Result.Ok("John")
                        from lastName in Result.Ok("Doe")
                        select $"{firstName} {lastName}";
            Assert.True(query.IsSuccess);
            Assert.Equal("John Doe", query.Value);
        }

        [Fact]
        public void QuerySyntax_FullPipeline_WithValidation()
        {
            var query = from input in Result.Ok("  test@example.com  ")
                        let trimmed = input.Trim()
                        from validated in Result.Ok(trimmed).Ensure(
                            s => s.Contains("@"),
                            new Error("INVALID", "not an email"))
                        select validated.ToUpper();

            Assert.True(query.IsSuccess);
            Assert.Equal("TEST@EXAMPLE.COM", query.Value);
        }

        [Fact]
        public void QuerySyntax_FullPipeline_ValidationFails()
        {
            var query = from input in Result.Ok("invalid")
                        let trimmed = input.Trim()
                        from validated in Result.Ok(trimmed).Ensure(
                            s => s.Contains("@"),
                            new Error("INVALID", "not an email"))
                        select validated.ToUpper();

            Assert.True(query.IsFailed);
            Assert.Equal("INVALID", query.Error.Code);
        }

        [Fact]
        public void QuerySyntax_WithTypedError_Pipeline()
        {
            var query = from x in Result.Ok<int, TestError>(10)
                        from y in Result.Ok<int, TestError>(5)
                        select x - y;

            Assert.True(query.IsSuccess);
            Assert.Equal(5, query.Value);
        }

        [Fact]
        public void QuerySyntax_MixedSyncOperations()
        {
            var query = from x in Result.Ok(2)
                        from y in Result.Ok(3)
                        from z in Result.Ok(x + y)
                        let doubled = z * 2
                        select doubled;

            Assert.True(query.IsSuccess);
            Assert.Equal(10, query.Value);
        }
    }

    #endregion
}
