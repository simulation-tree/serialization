using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.TOML
{
    [SkipLocalsInit]
    public unsafe struct TOMLObject : IDisposable, ISerializable
    {
        private Implementation* tomlObject;

        public readonly ReadOnlySpan<TOMLKeyValue> KeyValues
        {
            get
            {
                MemoryAddress.ThrowIfDefault(tomlObject);

                return tomlObject->keyValues.AsSpan();
            }
        }

        public readonly ReadOnlySpan<TOMLTable> Tables
        {
            get
            {
                MemoryAddress.ThrowIfDefault(tomlObject);

                return tomlObject->tables.AsSpan();
            }
        }

        public readonly bool IsDisposed => tomlObject == default;

#if NET
        /// <summary>
        /// Creates an empty TOML object.
        /// </summary>
        public TOMLObject()
        {
            tomlObject = MemoryAddress.AllocatePointer<Implementation>();
            tomlObject->keyValues = new(4);
            tomlObject->tables = new(4);
        }
#endif

        public TOMLObject(void* pointer)
        {
            this.tomlObject = (Implementation*)pointer;
        }

        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            Span<TOMLKeyValue> keyValues = tomlObject->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.ToString(destination);
            }

            Span<TOMLTable> tables = tomlObject->tables.AsSpan();
            foreach (TOMLTable table in tables)
            {
                table.ToString(destination);
            }
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            Span<TOMLKeyValue> keyValues = tomlObject->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.Dispose();
            }

            tomlObject->keyValues.Dispose();
            Span<TOMLTable> tables = tomlObject->tables.AsSpan();
            foreach (TOMLTable table in tables)
            {
                table.Dispose();
            }

            tomlObject->tables.Dispose();
            MemoryAddress.Free(ref tomlObject);
        }

        public readonly void Add(ReadOnlySpan<char> key, ReadOnlySpan<char> text)
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            TOMLKeyValue keyValue = new(key, text);
            tomlObject->keyValues.Add(keyValue);
        }

        public readonly void Add(ReadOnlySpan<char> key, double number)
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            TOMLKeyValue keyValue = new(key, number);
            tomlObject->keyValues.Add(keyValue);
        }

        public readonly void Add(ReadOnlySpan<char> key, bool boolean)
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            TOMLKeyValue keyValue = new(key, boolean);
            tomlObject->keyValues.Add(keyValue);
        }

        public readonly bool ContainsValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            Span<TOMLKeyValue> keyValues = tomlObject->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlObject);

            Span<TOMLKeyValue> keyValues = tomlObject->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlObject);
            ThrowIfValueIsMissing(key);

            Span<TOMLKeyValue> keyValues = tomlObject->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlObject);

            Span<TOMLTable> tables = tomlObject->tables.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlObject);

            Span<TOMLTable> tables = tomlObject->tables.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlObject);
            ThrowIfTableIsMissing(name);

            Span<TOMLTable> tables = tomlObject->tables.AsSpan();
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
            tomlObject = MemoryAddress.AllocatePointer<Implementation>();
            tomlObject->keyValues = new(4);
            tomlObject->tables = new(4);
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
                    tomlObject->keyValues.Add(keyValue);
                }
                else if (token.type == Token.Type.StartSquareBracket)
                {
                    TOMLTable table = byteReader.ReadObject<TOMLTable>();
                    tomlObject->tables.Add(table);
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
                throw new ArgumentException($"Key '{key.ToString()}' is missing in TOML object", nameof(key));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTableIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsTable(name))
            {
                throw new ArgumentException($"Table '{name.ToString()}' is missing in TOML object", nameof(name));
            }
        }

        /// <summary>
        /// Creates a new empty TOML object.
        /// </summary>
        public static TOMLObject Create()
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