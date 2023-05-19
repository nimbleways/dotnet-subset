using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nimbleways.Tools.Subset.Utils;

internal static class GuardExtensions
{
    // ! Value is provided at compile-time
    [return: NotNull]
    public static T AsNotNull<T>(this T? obj, [CallerArgumentExpression("obj")] string expression = null!)
        where T : class?
    {
        return obj is null ? throw new UnexpectedNullExpressionException(expression) : obj;
    }
}

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "<Pending>")]
public class UnexpectedNullExpressionException : Exception
{
    public UnexpectedNullExpressionException(string expression)
        : base($"Expression '{expression}' is null")
    {
    }
}
