using Collections.Generic;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Serialization.TOML
{
    public unsafe struct TOMLTable : IDisposable, ISerializable
    {
        internal Implementation* table;

        public readonly ReadOnlySpan<char> Name
        {
            get
            {
                MemoryAddress.ThrowIfDefault(table);

                return table->name.GetSpan<char>(table->nameLength);
            }
        }

        public readonly ReadOnlySpan<TOMLKeyValue> KeyValues
        {
            get
            {
                MemoryAddress.ThrowIfDefault(table);

                return table->keyValues.AsSpan();
            }
        }

        public readonly bool IsDisposed => table == default;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public TOMLTable()
        {
        }
#endif

        public TOMLTable(ReadOnlySpan<char> name)
        {
            table = MemoryAddress.AllocatePointer<Implementation>();
            table->name = MemoryAddress.Allocate(name.Length * sizeof(char));
            table->keyValues = new(4);
            table->nameLength = name.Length;
        }
        
        public TOMLTable(void* pointer)
        {
            this.table = (Implementation*)pointer;
        }

        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        public readonly void ToString(Text destination)
        {
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(table);

            table->name.Dispose();

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.Dispose();
            }

            table->keyValues.Dispose();
            MemoryAddress.Free(ref table);
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            table = MemoryAddress.AllocatePointer<Implementation>();
            table->keyValues = new(4);

            TOMLReader tomlReader = new(byteReader);
            tomlReader.ReadToken(); //[
            Token nameToken = tomlReader.ReadToken();
            tomlReader.ReadToken(); //]

            Span<char> nameBuffer = stackalloc char[nameToken.length * 4];
            table->nameLength = tomlReader.GetText(nameToken, nameBuffer);
            table->name = MemoryAddress.Allocate(nameBuffer.Slice(0, table->nameLength));

            while (tomlReader.PeekToken(out Token nextToken))
            {
                if (nextToken.type == Token.Type.Hash)
                {
                    tomlReader.ReadToken(); //#
                    tomlReader.ReadToken(); //text
                }
                else if (nextToken.type == Token.Type.Text)
                {
                    TOMLKeyValue keyValue = byteReader.ReadObject<TOMLKeyValue>();
                    table->keyValues.Add(keyValue);
                }
                else if (nextToken.type == Token.Type.StartSquareBracket)
                {
                    break;
                }
                else
                {
                    tomlReader.ReadToken();
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
            using Text destination = new(0);
            ToString(destination);
            byteWriter.WriteUTF8(destination.AsSpan());
        }

        public readonly bool ContainsValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(table);

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(table);

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(table);
            ThrowIfValueIsMissing(key);

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                if (keyValue.Key.SequenceEqual(key))
                {
                    return keyValue;
                }
            }

            return default;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfValueIsMissing(ReadOnlySpan<char> key)
        {
            if (!ContainsValue(key))
            {
                throw new ArgumentException($"Key '{key.ToString()}' is missing in TOML object", nameof(key));
            }
        }

        internal struct Implementation
        {
            public int nameLength;
            public MemoryAddress name;
            public List<TOMLKeyValue> keyValues;
        }
    }
}