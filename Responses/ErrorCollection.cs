using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Responses;

/// <summary>
/// An immutable collection of errors for Results with multiple failures.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ErrorCollection : IReadOnlyList<IError>
{
    private static readonly ErrorCollection _empty = new(Array.Empty<IError>());

    // Nullable because default(ErrorCollection) bypasses every constructor.
    private readonly IError[]? _errors;

    /// <summary>
    /// Gets an empty error collection.
    /// </summary>
    public static ErrorCollection Empty => _empty;

    /// <summary>
    /// Creates a new error collection with the specified errors.
    /// </summary>
    public ErrorCollection(params IError[] errors)
    {
        // Defensive copy: an immutable collection must not alias an array the caller
        // still holds and can mutate afterwards.
        _errors = errors is { Length: > 0 } ? (IError[])errors.Clone() : Array.Empty<IError>();
    }

    /// <summary>
    /// Creates a new error collection from a sequence of errors.
    /// </summary>
    public ErrorCollection(IEnumerable<IError> errors)
    {
        _errors = errors?.ToArray() ?? Array.Empty<IError>();
    }

    private IError[] Errors => _errors ?? Array.Empty<IError>();

    /// <inheritdoc />
    public int Count => Errors.Length;

    /// <inheritdoc />
    public IError this[int index] => Errors[index];

    /// <inheritdoc />
    public IEnumerator<IError> GetEnumerator() => ((IEnumerable<IError>)Errors).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
