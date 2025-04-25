using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.JSON
{
    [SkipLocalsInit]
    public struct JSONWriter : IDisposable
    {
        private readonly SerializationSettings settings;
        private readonly ByteWriter writer;
        private Token last;
        private int depth;

        public readonly bool IsDisposed => writer.IsDisposed;
        public readonly int Position => writer.Position;

        public JSONWriter()
        {
            settings = default;
            writer = new ByteWriter(4);
            last = default;
        }

        public JSONWriter(SerializationSettings settings)
        {
            this.settings = settings;
            this.writer = new ByteWriter(4);
            last = default;
        }

        public override readonly string ToString()
        {
            ByteReader reader = new(AsSpan());
            Text tempBuffer = new(Position * 2);
            Span<char> buffer = tempBuffer.AsSpan();
            int read = reader.ReadUTF8(buffer);
            reader.Dispose();
            string result = buffer.Slice(0, read).ToString();
            tempBuffer.Dispose();
            return result;
        }

        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return writer.AsSpan();
        }

        public readonly void Dispose()
        {
            writer.Dispose();
        }

        public void WriteStartObject()
        {
            if (last.type == Token.Type.EndObject)
            {
                writer.WriteUTF8(',');
                settings.NewLine(writer);
            }

            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char), Token.Type.StartObject);
            writer.WriteUTF8('{');
            settings.NewLine(writer);
            depth++;
        }

        public void WriteEndObject()
        {
            depth--;
            settings.NewLine(writer);
            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char), Token.Type.EndObject);
            writer.WriteUTF8('}');
        }

        public void WriteStartArray()
        {
            last = new(writer.Position, sizeof(char), Token.Type.StartArray);
            writer.WriteUTF8('[');
            settings.NewLine(writer);
            depth++;
        }

        public void WriteEndArray()
        {
            depth--;
            settings.NewLine(writer);
            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char), Token.Type.EndArray);
            writer.WriteUTF8(']');
        }

        private void WriteText(ReadOnlySpan<char> value, SerializationSettings settings)
        {
            last = new(writer.Position, sizeof(char) * (2 + value.Length), Token.Type.Text);
            settings.WriteTextQuoteCharacter(writer);
            writer.WriteUTF8(value);
            settings.WriteTextQuoteCharacter(writer);
        }

        /// <summary>
        /// Writes the given text value assuming its an element inside an array.
        /// </summary>
        public void WriteTextElement(ReadOnlySpan<char> value)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8(',');
                settings.NewLine(writer);
            }

            WriteText(value, settings);
        }

        public void WriteNumber(double number)
        {
            Span<char> buffer = stackalloc char[32];
            int length = number.ToString(buffer);

            last = new(writer.Position, sizeof(char) * length, Token.Type.Text);
            writer.WriteUTF8(buffer.Slice(0, length));
        }

        public void WriteBoolean(bool value)
        {
            if (value)
            {
                last = new(writer.Position, sizeof(char) * 4, Token.Type.Text);
                writer.WriteUTF8(Token.True);
            }
            else
            {
                last = new(writer.Position, sizeof(char) * 5, Token.Type.Text);
                writer.WriteUTF8(Token.False);
            }
        }

        public void WriteNull()
        {
            last = new(writer.Position, sizeof(char) * 4, Token.Type.Text);
            writer.WriteUTF8(Token.Null);
        }

        public void WriteObject<T>(T obj) where T : unmanaged, IJSONSerializable
        {
            WriteStartObject();
            obj.Write(ref this);
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
                settings.NewLine(writer);
            }

            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char) * (2 + name.Length), Token.Type.Text);
            settings.WriteNameQuoteCharacter(writer);
            writer.WriteUTF8(name);
            settings.WriteNameQuoteCharacter(writer);
            writer.WriteUTF8(':');
            settings.SpaceAfterColon(writer);
        }

        public void WriteName(string name)
        {
            WriteName(name.AsSpan());
        }

        public void WriteArray<T>(ReadOnlySpan<char> name, ReadOnlySpan<T> items) where T : unmanaged, IJSONSerializable
        {
            WriteName(name);
            WriteStartArray();
            foreach (T item in items)
            {
                WriteObject(item);
            }

            WriteEndArray();
        }

        public void WriteArray<T>(string name, ReadOnlySpan<T> items) where T : unmanaged, IJSONSerializable
        {
            WriteArray(name.AsSpan(), items);
        }

        public void WriteProperty(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            WriteName(name);
            WriteText(text, settings);
        }

        public void WriteProperty(string name, ReadOnlySpan<char> text)
        {
            WriteProperty(name.AsSpan(), text);
        }

        public void WriteProperty(ReadOnlySpan<char> name, double number)
        {
            WriteName(name);
            WriteNumber(number);
        }

        public void WriteProperty(string name, double number)
        {
            WriteProperty(name.AsSpan(), number);
        }

        public void WriteProperty(ReadOnlySpan<char> name, bool boolean)
        {
            WriteName(name);
            WriteBoolean(boolean);
        }

        public void WriteProperty(string name, bool boolean)
        {
            WriteProperty(name.AsSpan(), boolean);
        }

        public void WriteProperty<T>(ReadOnlySpan<char> name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteName(name);
            WriteObject(obj);
        }

        public void WriteProperty<T>(string name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteProperty(name.AsSpan(), obj);
        }

        public static JSONWriter Create(SerializationSettings settings = default)
        {
            return new(settings);
        }
    }
}