using System.Collections;

namespace Xunit;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FactAttribute : Attribute
{
}

public static class Assert
{
    public static void False(bool condition)
    {
        if (condition)
        {
            Fail("Expected false.");
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Fail($"Expected '{expected}', actual '{actual}'.");
        }
    }

    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var expectedArray = expected.ToArray();
        var actualArray = actual.ToArray();

        if (expectedArray.Length != actualArray.Length)
        {
            Fail($"Expected sequence length {expectedArray.Length}, actual {actualArray.Length}.");
        }

        for (var i = 0; i < expectedArray.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(expectedArray[i], actualArray[i]))
            {
                Fail($"Expected item {i} to be '{expectedArray[i]}', actual '{actualArray[i]}'.");
            }
        }
    }

    public static T Single<T>(IEnumerable<T> values)
    {
        var array = values.ToArray();
        if (array.Length != 1)
        {
            Fail($"Expected a single item, actual count {array.Length}.");
        }

        return array[0];
    }

    public static void Contains(string expectedSubstring, string actualString)
    {
        if (actualString is null || !actualString.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            Fail($"Expected string to contain '{expectedSubstring}'.");
        }
    }

    public static void Contains<T>(T expected, IEnumerable<T> values)
    {
        if (!values.Contains(expected))
        {
            Fail($"Expected collection to contain '{expected}'.");
        }
    }

    public static void Contains<T>(IEnumerable<T> values, Predicate<T> predicate)
    {
        if (!values.Any(value => predicate(value)))
        {
            Fail("Expected collection to contain a matching item.");
        }
    }

    public static void DoesNotContain<T>(IEnumerable<T> values, Predicate<T> predicate)
    {
        if (values.Any(value => predicate(value)))
        {
            Fail("Expected collection not to contain a matching item.");
        }
    }

    public static void NotNull(object? value)
    {
        if (value is null)
        {
            Fail("Expected a non-null value.");
        }
    }

    private static void Fail(string message)
    {
        throw new AssertionException(message);
    }
}

public sealed class AssertionException : Exception
{
    public AssertionException(string message)
        : base(message)
    {
    }
}
