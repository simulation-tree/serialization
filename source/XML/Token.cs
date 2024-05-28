using System;

namespace Unmanaged.XML
{
    public readonly struct Token(uint position, uint length, Token.Type type)
    {
        public readonly uint position = position;
        public readonly uint length = length;
        public readonly Type type = type;

        public readonly uint End => position + length;

        public readonly override string ToString()
        {
            return $"Token(type: {type} position:{position} length:{length})";
        }

        public readonly string ToString(XMLReader reader)
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
                Span<char> buffer = stackalloc char[(int)length];
                int l = reader.GetText(this, buffer);
                return new string(buffer[..l]);
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