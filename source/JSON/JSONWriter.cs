using Collections;
using System;

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

        public unsafe override readonly string ToString()
        {
            BinaryReader reader = new(AsSpan());
            Array<char> tempArray = new(Position * 2);
            USpan<char> buffer = tempArray.AsSpan();
            uint read = reader.ReadUTF8Span(buffer);
            reader.Dispose();
            string result = buffer.Slice(0, read).ToString();
            tempArray.Dispose();
            return result;
        }

        public readonly USpan<byte> AsSpan()
        {
            return writer.GetBytes();
        }

        public readonly void Dispose()
        {
            writer.Dispose();
        }

        public void WriteStartObject()
        {
            last = new(writer.Position, sizeof(char), Token.Type.StartObject);
            writer.WriteUTF8Character('{');
        }

        public void WriteEndObject()
        {
            last = new(writer.Position, sizeof(char), Token.Type.EndObject);
            writer.WriteUTF8Character('}');
        }

        public void WriteStartArray()
        {
            last = new(writer.Position, sizeof(char), Token.Type.StartArray);
            writer.WriteUTF8Character('[');
        }

        public void WriteEndArray()
        {
            last = new(writer.Position, sizeof(char), Token.Type.EndArray);
            writer.WriteUTF8Character(']');
        }

        private void WriteText(USpan<char> value)
        {
            last = new(writer.Position, sizeof(char) * (2 + value.Length), Token.Type.Text);
            writer.WriteUTF8Character('"');
            writer.WriteUTF8Text(value);
            writer.WriteUTF8Character('"');
        }

        /// <summary>
        /// Writes the given text value assuming its an element inside an array.
        /// </summary>
        public void WriteTextElement(USpan<char> value)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8Character(',');
            }

            WriteText(value);
        }

        public void WriteNumber(double number)
        {
            USpan<char> buffer = stackalloc char[32];
            uint length = number.ToString(buffer);

            last = new(writer.Position, sizeof(char) * length, Token.Type.Number);
            writer.WriteUTF8Text(buffer.Slice(0, length));
        }

        public void WriteBoolean(bool value)
        {
            if (value)
            {
                last = new(writer.Position, sizeof(char) * 4, Token.Type.True);
                writer.WriteUTF8Text("true".AsUSpan());
            }
            else
            {
                last = new(writer.Position, sizeof(char) * 5, Token.Type.False);
                writer.WriteUTF8Text("false".AsUSpan());
            }
        }

        public void WriteNull()
        {
            last = new(writer.Position, sizeof(char) * 4, Token.Type.Null);
            writer.WriteUTF8Text("null".AsUSpan());
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
        public void WriteName(USpan<char> name)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8Character(',');
            }

            WriteText(name);
            writer.WriteUTF8Character(':');
        }

        public void WriteName(string name)
        {
            WriteName(name.AsUSpan());
        }

        public void WriteProperty(USpan<char> name, USpan<char> text)
        {
            WriteName(name);
            WriteText(text);
        }

        public void WriteProperty(string name, USpan<char> text)
        {
            WriteProperty(name.AsUSpan(), text);
        }

        public void WriteProperty(USpan<char> name, double number)
        {
            WriteName(name);
            WriteNumber(number);
        }

        public void WriteProperty(string name, double number)
        {
            WriteProperty(name.AsUSpan(), number);
        }

        public void WriteProperty(USpan<char> name, bool boolean)
        {
            WriteName(name);
            WriteBoolean(boolean);
        }

        public void WriteProperty(string name, bool boolean)
        {
            WriteProperty(name.AsUSpan(), boolean);
        }

        public void WriteProperty<T>(USpan<char> name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteName(name);
            WriteObject(obj);
        }

        public void WriteProperty<T>(string name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteProperty(name.AsUSpan(), obj);
        }

        public static JSONWriter Create()
        {
            return new(BinaryWriter.Create());
        }
    }
}