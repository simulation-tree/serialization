using System;

namespace Unmanaged.XML
{
    public readonly struct XMLWriter : IDisposable
    {
        private readonly BinaryWriter writer;

        public readonly bool IsDisposed => writer.IsDisposed;

        public XMLWriter()
        {
            writer = new();
        }

        public readonly void Dispose()
        {
            writer.Dispose();
        }

        public readonly void WriteStartObject()
        {
            writer.WriteUTF8('<');
        }

        public readonly void WriteEndObject()
        {
            writer.WriteUTF8('>');
        }

        public readonly void WriteSlash()
        {
            writer.WriteUTF8('/');
        }

        public readonly void WriteAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            writer.WriteUTF8Span(name);
            writer.WriteUTF8('=');
            writer.WriteUTF8('"');
            writer.WriteUTF8Span(value);
            writer.WriteUTF8('"');
        }

        public readonly void WriteText(ReadOnlySpan<char> value)
        {
            writer.WriteUTF8Span(value);
        }
    }
}