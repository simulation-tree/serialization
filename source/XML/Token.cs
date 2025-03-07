using Unmanaged;

namespace Serialization.XML
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

        public readonly string ToString(XMLReader reader)
        {
            using Text buffer = new(0);
            ToString(reader, buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Adds the string representation of this token to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values added.</returns>
        public readonly uint ToString(XMLReader reader, Text destination)
        {
            switch (type)
            {
                case Type.Open:
                    destination.Append('<');
                    return 1;
                case Type.Close:
                    destination.Append('>');
                    return 1;
                case Type.Slash:
                    destination.Append('/');
                    return 1;
                case Type.Text:
                    return reader.GetText(this, destination);
                case Type.Prologue:
                    destination.Append('?');
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