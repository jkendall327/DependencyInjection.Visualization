namespace DependencyInjection.Visualization;

public static class TypeExtensions
{
    /// <summary>
    /// Determines if two types match, considering both exact matches and open/closed generic type relationships.
    /// </summary>
    /// <remarks>
    /// This method handles exact and open-to-closed generic type matching.
    /// For example, it will return true for string and string (exact match) as well as List&lt;int&gt; and List&lt;&gt; (closed generic to open generic).
    /// </remarks>
    /// <param name="closed">The closed type to compare.</param>
    /// <param name="open">The open generic type definition to compare.</param>
    /// <returns>True if the types match according to the rules described; otherwise, false.</returns>
    internal static bool IsInstanceOfGenericTypeDefinition(this Type closed, Type open)
    {
        if (closed == open)
        {
            return true;
        }

        // 'Type definition' is the term of art for open generic.
        // E.g. b here is ILogger<>.
        return open.IsGenericTypeDefinition &&
               // a is a closed generic, i.e. ILogger<MyClass>.
               closed.IsGenericType &&
               // The generic type definition of ILogger<MyClass> is ILogger<>.
               // Therefore a matches b.
               closed.GetGenericTypeDefinition() == open;
    }
}