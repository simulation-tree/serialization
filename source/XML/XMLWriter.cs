using System;
using Unmanaged.Collections;

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

        public XMLWriter(UnmanagedList<byte> data)
        {
            writer = new(data);
        }

        public readonly void Dispose()
        {
            writer.Dispose();
        }

        public readonly void WriteStartObject()
        {
            writer.WriteValue('<');
        }

        public readonly void WriteEndObject()
        {
            writer.WriteValue('>');
        }

        public readonly void WriteSlash()
        {
            writer.WriteValue('/');
        }

        public readonly void WriteAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            writer.WriteSpan(name);
            writer.WriteValue('=');
            writer.WriteValue('"');
            writer.WriteSpan(value);
            writer.WriteValue('"');
        }

        public readonly void WriteText(ReadOnlySpan<char> value)
        {
            writer.WriteSpan(value);
        }
    }
}