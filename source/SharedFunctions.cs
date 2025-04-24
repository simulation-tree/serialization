namespace Serialization
{
    internal static class SharedFunctions
    {
        private const char BOM = (char)65279;

        public static bool IsWhitespace(char character)
        {
            return character == ' ' || character == '\t' || character == '\n' || character == '\r' || character == BOM;
        }
    }
}