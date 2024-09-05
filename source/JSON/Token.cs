namespace Unmanaged.JSON
{
    public readonly struct Token
    {
        public readonly uint position;
        public readonly uint length;
        public readonly Type type;

        public Token(uint position, uint length, Token.Type type)
        {
            this.position = position;
            this.length = length;
            this.type = type;
        }

        public override string ToString()
        {
            return $"JSONToken (type: {type}, position: {position}, length: {length})";
        }

        public unsafe readonly string ToString(JSONReader reader)
        {
            USpan<char> buffer = stackalloc char[(int)length];
            uint read = reader.GetText(this, buffer);
            return new string(buffer.pointer, 0, (int)read);
        }

        public enum Type : byte
        {
            Unknown,
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
