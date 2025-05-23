﻿using System;
using Unmanaged;

namespace Serialization.XML
{
    public readonly struct XMLWriter : IDisposable
    {
        private readonly ByteWriter writer;

        public readonly bool IsDisposed => writer.IsDisposed;

#if NET
        public XMLWriter()
        {
            writer = new(4);
        }
#endif

        private XMLWriter(ByteWriter writer)
        {
            this.writer = writer;
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
            writer.WriteUTF8(name);
            writer.WriteUTF8('=');
            writer.WriteUTF8('"');
            writer.WriteUTF8(value);
            writer.WriteUTF8('"');
        }

        public readonly void WriteAttribute(string name, string value)
        {
            writer.WriteUTF8(name);
            writer.WriteUTF8('=');
            writer.WriteUTF8('"');
            writer.WriteUTF8(value);
            writer.WriteUTF8('"');
        }

        public readonly void WriteText(ReadOnlySpan<char> value)
        {
            writer.WriteUTF8(value);
        }

        public readonly void WriteText(string value)
        {
            writer.WriteUTF8(value);
        }

        public static XMLWriter Create()
        {
            return new(new(4));
        }
    }
}