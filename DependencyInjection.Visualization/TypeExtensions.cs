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
    /// <param name="a">The first type to compare, often a closed generic type if 'b' is open.</param>
    /// <param name="b">The second type to compare, potentially an open generic type definition.</param>
    /// <returns>True if the types match according to the rules described; otherwise, false.</returns>
    internal static bool MatchesGenerically(this Type a, Type b)
    {
        if (a == b)
        {
            return true;
        }

        return b.IsGenericTypeDefinition &&
               a.IsGenericType &&
               a.GetGenericTypeDefinition() == b;
    }
}