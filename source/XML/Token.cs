namespace Unmanaged.XML
{
    public readonly struct Token(uint position, uint length, Token.Type type)
    {
        public readonly uint position = position;
        public readonly uint length = length;
        public readonly Type type = type;

        public enum Type : byte
        {
            Unknown,
            Open,
            Close,
            Slash,
            Text
        }
    }
}