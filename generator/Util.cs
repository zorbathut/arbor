namespace Arbor
{
    using Microsoft.CodeAnalysis;

    internal static class Util
    {
        public static string RemoveSuffix(this string input, string suffix)
        {
            if (!input.EndsWith(suffix))
            {
                return null;
            }

            return input.Substring(0, input.Length - suffix.Length);
        }

        public static bool InheritsFrom(this ITypeSymbol derivedType, ITypeSymbol baseType)
        {
            var currentType = derivedType;
            while (currentType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentType, baseType))
                    return true;

                currentType = currentType.BaseType;
            }

            return false;
        }
    }
}
