using Unmanaged;

namespace Serialization.TOML
{
    public readonly struct Token
    {
        public const string Tokens = "#=,[]{}";

        public readonly int position;
        public readonly int length;
        public readonly Type type;

        public readonly int End => position + length;

        public Token(int position, int length, Type type)
        {
            this.position = position;
            this.length = length;
            this.type = type;
        }

        public readonly override string ToString()
        {
            return $"Token(type: {type} position:{position} length:{length})";
        }

        public readonly string ToString(TOMLReader reader)
        {
            using Text destination = new(4);
            ToString(reader, destination);
            return destination.ToString();
        }

        /// <summary>
        /// Adds the string representation of this token to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values added.</returns>
        public readonly int ToString(TOMLReader reader, Text destination)
        {
            switch (type)
            {
                case Type.Text:
                    return reader.GetText(this, destination);
                case Type.Hash:
                    destination.Append('#');
                    return 1;
                case Type.Equals:
                    destination.Append('=');
                    return 1;
                case Type.Comma:
                    destination.Append(',');
                    return 1;
                case Type.StartSquareBracket:
                    destination.Append('[');
                    return 1;
                case Type.EndSquareBracket:
                    destination.Append(']');
                    return 1;
                case Type.StartCurlyBrace:
                    destination.Append('{');
                    return 1;
                case Type.EndCurlyBrace:
                    destination.Append('}');
                    return 1;
                default:
                    return 0;
            }
        }

        public enum Type : byte
        {
            Unknown,
            Text,
            Hash,
            Equals,
            Comma,
            StartSquareBracket,
            EndSquareBracket,
            StartCurlyBrace,
            EndCurlyBrace
        }
    }
}