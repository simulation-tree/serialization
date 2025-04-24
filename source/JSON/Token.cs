using System;

namespace Serialization.JSON
{
    public readonly struct Token
    {
        public readonly int position;
        public readonly int length;
        public readonly Type type;

        public Token(int position, int length, Type type)
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
            Span<char> buffer = stackalloc char[length];
            int read = reader.GetText(this, buffer);
            return buffer.Slice(0, read).ToString();
        }

        public enum Type : byte
        {
            Unknown,
            StartObject,
            EndObject,
            StartArray,
            EndArray,
            Text,
        }
    }
}
