using System;

namespace Unmanaged.JSON
{
    public struct JSONWriter : IDisposable
    {
        private readonly BinaryWriter writer;
        private Token last;

        public readonly bool IsDisposed => writer.IsDisposed;

        public JSONWriter()
        {
            writer = new();
        }

        public JSONWriter(BinaryWriter writer)
        {
            this.writer = writer;
        }

        public override readonly string ToString()
        {
            return AsSpan().ToString();
        }

        public unsafe readonly ReadOnlySpan<char> AsSpan()
        {
            uint length = writer.Length / sizeof(char);
            return new((void*)writer.Address, (int)length);
        }

        public readonly Span<byte> GetBytes()
        {
            return writer.AsSpan();
        }

        public readonly void Dispose()
        {
            writer.Dispose();
        }

        public void WriteStartObject()
        {
            last = new(writer.Length, sizeof(char), Token.Type.StartObject);
            writer.WriteValue('{');
        }

        public void WriteEndObject()
        {
            last = new(writer.Length, sizeof(char), Token.Type.EndObject);
            writer.WriteValue('}');
        }

        public void WriteStartArray()
        {
            last = new(writer.Length, sizeof(char), Token.Type.StartArray);
            writer.WriteValue('[');
        }

        public void WriteEndArray()
        {
            last = new(writer.Length, sizeof(char), Token.Type.EndArray);
            writer.WriteValue(']');
        }

        public void WriteText(ReadOnlySpan<char> value)
        {
            last = new(writer.Length, (uint)(sizeof(char) * (2 + value.Length)), Token.Type.Text);
            writer.WriteValue('"');
            writer.WriteSpan(value);
            writer.WriteValue('"');
        }

        public void WriteNumber(double number)
        {
            Span<char> buffer = stackalloc char[32];
            number.TryFormat(buffer, out int charsWritten);

            last = new(writer.Length, (uint)(sizeof(char) * charsWritten), Token.Type.Number);
            writer.WriteSpan<char>(buffer[..charsWritten]);
        }

        public void WriteBoolean(bool value)
        {
            if (value)
            {
                last = new(writer.Length, sizeof(char) * 4, Token.Type.True);
                writer.WriteSpan("true".AsSpan());
            }
            else
            {
                last = new(writer.Length, sizeof(char) * 5, Token.Type.False);
                writer.WriteSpan("false".AsSpan());
            }
        }

        public void WriteNull()
        {
            last = new(writer.Length, sizeof(char) * 4, Token.Type.Null);
            writer.WriteSpan("null".AsSpan());
        }

        public readonly void WriteObject<T>(T obj) where T : unmanaged, IJSONObject
        {
            writer.WriteValue('{');
            obj.Write(this);
            writer.WriteValue('}');
        }

        /// <summary>
        /// Writes only the name of the property.
        /// </summary>
        public void WriteName(ReadOnlySpan<char> name)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray)
            {
                writer.WriteValue(',');
            }

            WriteText(name);
            writer.WriteValue(':');
        }

        public void WriteProperty(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            WriteName(name);
            WriteText(text);
        }

        public void WriteProperty(ReadOnlySpan<char> name, double number)
        {
            WriteName(name);
            WriteNumber(number);
        }

        public void WriteProperty(ReadOnlySpan<char> name, bool boolean)
        {
            WriteName(name);
            WriteBoolean(boolean);
        }

        public void WriteProperty<T>(ReadOnlySpan<char> name, T obj) where T : unmanaged, IJSONObject
        {
            WriteName(name);
            WriteObject(obj);
        }
    }
}