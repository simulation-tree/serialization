using System;
using Unmanaged.Collections;

namespace Unmanaged.JSON
{
    public struct JSONWriter : IDisposable
    {
        private readonly BinaryWriter writer;
        private Token last;

        public readonly bool IsDisposed => writer.IsDisposed;
        public readonly uint Position => writer.Position;

#if NET
        [Obsolete("Default constructor not available", true)]
        public JSONWriter()
        {
            throw new NotImplementedException();
        }
#endif

        public JSONWriter(BinaryWriter writer)
        {
            this.writer = writer;
            last = default;
        }

        public override readonly string ToString()
        {
            using BinaryReader reader = new(AsSpan());
            using UnmanagedArray<char> buffer = new(Position * 2);
            Span<char> bufferSpan = buffer.AsSpan();
            int length = reader.ReadUTF8Span(bufferSpan);
            return new(bufferSpan[..length]);
        }

        public readonly Span<byte> AsSpan()
        {
            return writer.AsSpan();
        }

        public readonly void Dispose()
        {
            writer.Dispose();
        }

        public void WriteStartObject()
        {
            last = new(writer.Position, sizeof(char), Token.Type.StartObject);
            writer.WriteUTF8('{');
        }

        public void WriteEndObject()
        {
            last = new(writer.Position, sizeof(char), Token.Type.EndObject);
            writer.WriteUTF8('}');
        }

        public void WriteStartArray()
        {
            last = new(writer.Position, sizeof(char), Token.Type.StartArray);
            writer.WriteUTF8('[');
        }

        public void WriteEndArray()
        {
            last = new(writer.Position, sizeof(char), Token.Type.EndArray);
            writer.WriteUTF8(']');
        }

        private void WriteText(ReadOnlySpan<char> value)
        {
            last = new(writer.Position, (uint)(sizeof(char) * (2 + value.Length)), Token.Type.Text);
            writer.WriteUTF8('"');
            writer.WriteUTF8Span(value);
            writer.WriteUTF8('"');
        }

        /// <summary>
        /// Writes the given text value assuming its an element inside an array.
        /// </summary>
        public void WriteTextElement(ReadOnlySpan<char> value)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8(',');
            }

            WriteText(value);
        }

        public void WriteNumber(double number)
        {
            Span<char> buffer = stackalloc char[32];
            number.TryFormat(buffer, out int charsWritten);

            last = new(writer.Position, (uint)(sizeof(char) * charsWritten), Token.Type.Number);
            writer.WriteUTF8Span(buffer[..charsWritten]);
        }

        public void WriteBoolean(bool value)
        {
            if (value)
            {
                last = new(writer.Position, sizeof(char) * 4, Token.Type.True);
                writer.WriteUTF8Span("true".AsSpan());
            }
            else
            {
                last = new(writer.Position, sizeof(char) * 5, Token.Type.False);
                writer.WriteUTF8Span("false".AsSpan());
            }
        }

        public void WriteNull()
        {
            last = new(writer.Position, sizeof(char) * 4, Token.Type.Null);
            writer.WriteUTF8Span("null".AsSpan());
        }

        public void WriteObject<T>(T obj) where T : unmanaged, IJSONSerializable
        {
            WriteStartObject();
            obj.Write(this);
            WriteEndObject();
        }

        /// <summary>
        /// Writes only the name of the property.
        /// </summary>
        public void WriteName(ReadOnlySpan<char> name)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8(',');
            }

            WriteText(name);
            writer.WriteUTF8(':');
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

        public void WriteProperty<T>(ReadOnlySpan<char> name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteName(name);
            WriteObject(obj);
        }

        public static JSONWriter Create()
        {
            return new(BinaryWriter.Create());
        }
    }
}