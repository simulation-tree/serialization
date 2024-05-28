namespace Unmanaged.JSON
{
    public readonly struct Token(uint position, uint length, Token.Type type)
    {
        public readonly uint position = position;
        public readonly uint length = length;
        public readonly Type type = type;

        public override string ToString()
        {
            return $"JSONToken (type: {type}, position: {position}, length: {length})";
        }

        public enum Type : byte
        {
            StartObject,
            EndObject,
            StartArray,
            EndArray,
            Text,
            Number,
            True,
            False,
            Null
        }
    }
}
