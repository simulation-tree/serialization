using Collections;

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
            using List<char> list = new(length);
            uint copied = ToString(reader, list);
            return list.AsSpan(0, copied).ToString();
        }

        /// <summary>
        /// Adds the string representation of this token to the list.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values added.</returns>
        public readonly uint ToString(XMLReader reader, List<char> list)
        {
            switch (type)
            {
                case Type.Open:
                    list.Add('<');
                    return 1;
                case Type.Close:
                    list.Add('>');
                    return 1;
                case Type.Slash:
                    list.Add('/');
                    return 1;
                case Type.Text:
                    return reader.GetText(this, list);
                case Type.Prologue:
                    list.Add('?');
                    return 1;
                default:
                    return 0;
            }
        }

        public enum Type : byte
        {
            Unknown,
            Open,
            Close,
            Slash,
            Text,
            Prologue
        }
    }
}