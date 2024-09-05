namespace Unmanaged.XML
{
    public readonly struct Token
    {
        public readonly uint position;
        public readonly uint length;
        public readonly Type type;

        public readonly uint End => position + length;

        public Token(uint position, uint length, Type type)
        {
            this.position = position;
            this.length = length;
            this.type = type;
        }

        public readonly override string ToString()
        {
            return $"Token(type: {type} position:{position} length:{length})";
        }

        public unsafe readonly string ToString(XMLReader reader)
        {
            if (type == Type.Open)
            {
                return "<";
            }
            else if (type == Type.Close)
            {
                return ">";
            }
            else if (type == Type.Slash)
            {
                return "/";
            }
            else if (type == Type.Text)
            {
                USpan<char> buffer = stackalloc char[(int)length];
                uint textLength = reader.GetText(this, buffer);
                return new string(buffer.pointer, 0, (int)textLength);
            }
            else
            {
                return string.Empty;
            }
        }

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