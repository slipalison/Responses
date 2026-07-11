using System;
using System.Threading.Tasks;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Verifies that every public composition method validates its delegate arguments,
/// throwing <see cref="ArgumentNullException"/> instead of failing later with NRE.
/// </summary>
public class GuardClauseTests
{
    private static readonly Error _error = new("ERR", "message");

    private static Result OkVoid => Result.Ok();
    private static Result<int> OkInt => Result.Ok(42);
    private static Result<int, Error> OkTyped => Result.Ok<int, Error>(42);

    [Fact]
    public void Result_Match_NullHandlers_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OkVoid.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => OkVoid.Match(() => { }, null!));
    }

    [Fact]
    public void Result_Tap_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkVoid.Tap(null!));

    [Fact]
    public void Result_Bind_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkVoid.Bind(null!));

    [Fact]
    public async Task Result_BindAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkVoid.BindAsync(null!));

    [Fact]
    public async Task Result_TapAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkVoid.TapAsync(null!));

    [Fact]
    public void ResultOfT_MatchAction_NullHandlers_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OkInt.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => OkInt.Match(_ => { }, null!));
    }

    [Fact]
    public void ResultOfT_ElseFunc_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkInt.Else((Func<Error, int>)null!));

    [Fact]
    public async Task ResultOfT_MapAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.MapAsync<int>(null!));

    [Fact]
    public async Task ResultOfT_BindAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.BindAsync<int>(null!));

    [Fact]
    public async Task ResultOfT_TapAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.TapAsync(null!));

    [Fact]
    public async Task ResultOfT_EnsureAsync_NullPredicate_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.EnsureAsync(null!, _error));

    [Fact]
    public async Task ResultOfT_MatchAsync_NullHandlers_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.MatchAsync(null!, _ => Task.FromResult(0)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.MatchAsync(_ => Task.FromResult(0), null!));
    }

    [Fact]
    public async Task ResultOfT_ElseAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkInt.ElseAsync(null!));

    [Fact]
    public void ResultOfT_SelectManyProjection_NullSelectors_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OkInt.SelectMany<int, int>(null!, (_, _) => 0));
        Assert.Throws<ArgumentNullException>(() => OkInt.SelectMany<int, int>(v => Result.Ok(v), null!));
    }

    [Fact]
    public void TypedResult_Map_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkTyped.Map<int>(null!));

    [Fact]
    public void TypedResult_Bind_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkTyped.Bind<int>(null!));

    [Fact]
    public void TypedResult_Tap_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkTyped.Tap(null!));

    [Fact]
    public void TypedResult_Ensure_NullPredicate_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkTyped.Ensure(null!, _error));

    [Fact]
    public void TypedResult_MatchFunc_NullHandlers_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OkTyped.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => OkTyped.Match<int>(_ => 0, null!));
    }

    [Fact]
    public void TypedResult_MatchAction_NullHandlers_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OkTyped.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => OkTyped.Match(_ => { }, null!));
    }

    [Fact]
    public void TypedResult_ElseFunc_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => OkTyped.Else((Func<Error, int>)null!));

    [Fact]
    public async Task TypedResult_MapAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.MapAsync<int>(null!));

    [Fact]
    public async Task TypedResult_BindAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.BindAsync<int>(null!));

    [Fact]
    public async Task TypedResult_TapAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.TapAsync(null!));

    [Fact]
    public async Task TypedResult_EnsureAsync_NullPredicate_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.EnsureAsync(null!, _error));

    [Fact]
    public async Task TypedResult_MatchAsync_NullHandlers_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.MatchAsync(null!, _ => Task.FromResult(0)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.MatchAsync(_ => Task.FromResult(0), null!));
    }

    [Fact]
    public async Task TypedResult_ElseAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => OkTyped.ElseAsync(null!));

    [Fact]
    public void TypedResult_SelectManyProjection_NullSelectors_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OkTyped.SelectMany<int, int>(null!, (_, _) => 0));
        Assert.Throws<ArgumentNullException>(() => OkTyped.SelectMany<int, int>(v => Result.Ok<int, Error>(v), null!));
    }

    [Fact]
    public void Result_Else_OnVoidResult_ThrowsWithStableMessage()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => OkVoid.Else(0));
        Assert.Contains("Result<void>", exception.Message);
    }
}
