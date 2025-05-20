namespace Serialization
{
    internal static class SharedFunctions
    {
        private const char BOM = (char)65279;

        public static bool IsWhitespace(char character)
        {
            return IsEndOfLine(character) || character == ' ' || character == '\t' || character == BOM;
        }

        public static bool IsEndOfLine(char character)
        {
            return character == '\n' || character == '\r';
        }
    }
}