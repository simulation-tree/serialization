using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.TOML
{
    [SkipLocalsInit]
    public unsafe struct TOMLDocument : IDisposable, ISerializable
    {
        private Implementation* document;

        public readonly ReadOnlySpan<TOMLKeyValue> KeyValues
        {
            get
            {
                MemoryAddress.ThrowIfDefault(document);

                return document->keyValues.AsSpan();
            }
        }

        public readonly ReadOnlySpan<TOMLTable> Tables
        {
            get
            {
                MemoryAddress.ThrowIfDefault(document);

                return document->tables.AsSpan();
            }
        }

        public readonly bool IsDisposed => document == default;

#if NET
        /// <summary>
        /// Creates an empty TOML document.
        /// </summary>
        public TOMLDocument()
        {
            document = MemoryAddress.AllocatePointer<Implementation>();
            document->keyValues = new(4);
            document->tables = new(4);
        }
#endif

        public TOMLDocument(void* pointer)
        {
            this.document = (Implementation*)pointer;
        }

        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.ToString(destination);
            }

            Span<TOMLTable> tables = document->tables.AsSpan();
            foreach (TOMLTable table in tables)
            {
                table.ToString(destination);
            }
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.Dispose();
            }

            document->keyValues.Dispose();
            Span<TOMLTable> tables = document->tables.AsSpan();
            foreach (TOMLTable table in tables)
            {
                table.Dispose();
            }

            document->tables.Dispose();
            MemoryAddress.Free(ref document);
        }

        public readonly void Add(ReadOnlySpan<char> key, ReadOnlySpan<char> text)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, text);
            document->keyValues.Add(keyValue);
        }

        public readonly void Add(ReadOnlySpan<char> key, double number)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, number);
            document->keyValues.Add(keyValue);
        }

        public readonly void Add(ReadOnlySpan<char> key, bool boolean)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, boolean);
            document->keyValues.Add(keyValue);
        }

        public readonly bool ContainsValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                if (keyValue.Key.SequenceEqual(key))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool TryGetValue(ReadOnlySpan<char> key, out TOMLKeyValue value)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                if (keyValue.Key.SequenceEqual(key))
                {
                    value = keyValue;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public readonly TOMLKeyValue GetValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(document);
            ThrowIfValueIsMissing(key);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                if (keyValue.Key.SequenceEqual(key))
                {
                    return keyValue;
                }
            }

            return default;
        }

        public readonly bool ContainsTable(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLTable> tables = document->tables.AsSpan();
            foreach (TOMLTable table in tables)
            {
                if (table.Name.SequenceEqual(name))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool TryGetTable(ReadOnlySpan<char> name, out TOMLTable table)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLTable> tables = document->tables.AsSpan();
            foreach (TOMLTable existingTable in tables)
            {
                if (existingTable.Name.SequenceEqual(name))
                {
                    table = existingTable;
                    return true;
                }
            }

            table = default;
            return false;
        }

        public readonly TOMLTable GetTable(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(document);
            ThrowIfTableIsMissing(name);

            Span<TOMLTable> tables = document->tables.AsSpan();
            foreach (TOMLTable table in tables)
            {
                if (table.Name.SequenceEqual(name))
                {
                    return table;
                }
            }

            return default;
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            document = MemoryAddress.AllocatePointer<Implementation>();
            document->keyValues = new(4);
            document->tables = new(4);
            TOMLReader tomlReader = new(byteReader);
            while (tomlReader.PeekToken(out Token token))
            {
                if (token.type == Token.Type.Hash)
                {
                    tomlReader.ReadToken(); //#
                    tomlReader.ReadToken(); //text
                }
                else if (token.type == Token.Type.Text)
                {
                    TOMLKeyValue keyValue = byteReader.ReadObject<TOMLKeyValue>();
                    document->keyValues.Add(keyValue);
                }
                else if (token.type == Token.Type.StartSquareBracket)
                {
                    TOMLTable table = byteReader.ReadObject<TOMLTable>();
                    document->tables.Add(table);
                }
                else
                {
                    tomlReader.ReadToken();
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
            using Text destination = new(32);
            ToString(destination);
            byteWriter.WriteUTF8(destination.AsSpan());
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfValueIsMissing(ReadOnlySpan<char> key)
        {
            if (!ContainsValue(key))
            {
                throw new ArgumentException($"Key '{key.ToString()}' is missing in TOML document", nameof(key));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTableIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsTable(name))
            {
                throw new ArgumentException($"Table '{name.ToString()}' is missing in TOML document", nameof(name));
            }
        }

        /// <summary>
        /// Creates an empty TOML document.
        /// </summary>
        public static TOMLDocument Create()
        {
            Implementation* tomlObject = MemoryAddress.AllocatePointer<Implementation>();
            tomlObject->keyValues = new(4);
            tomlObject->tables = new(4);
            return new(tomlObject);
        }

        private struct Implementation
        {
            public List<TOMLKeyValue> keyValues;
            public List<TOMLTable> tables;
        }
    }
}