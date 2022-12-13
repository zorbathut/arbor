namespace Arbor
{
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
    }
}
