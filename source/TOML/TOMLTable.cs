using Collections.Generic;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Serialization.TOML
{
    public unsafe struct TOMLTable : IDisposable, ISerializable
    {
        private Implementation* tomlTable;

        public readonly ReadOnlySpan<char> Name
        {
            get
            {
                MemoryAddress.ThrowIfDefault(tomlTable);

                return tomlTable->name.GetSpan<char>(tomlTable->nameLength);
            }
        }

        public readonly ReadOnlySpan<TOMLKeyValue> KeyValues
        {
            get
            {
                MemoryAddress.ThrowIfDefault(tomlTable);

                return tomlTable->keyValues.AsSpan();
            }
        }

        public readonly bool IsDisposed => tomlTable == default;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public TOMLTable()
        {
        }
#endif

        public TOMLTable(ReadOnlySpan<char> name)
        {
            tomlTable = MemoryAddress.AllocatePointer<Implementation>();
            tomlTable->name = MemoryAddress.Allocate(name.Length * sizeof(char));
            tomlTable->keyValues = new(4);
            tomlTable->nameLength = name.Length;
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
            MemoryAddress.ThrowIfDefault(tomlTable);

            tomlTable->name.Dispose();

            Span<TOMLKeyValue> keyValues = tomlTable->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.Dispose();
            }

            tomlTable->keyValues.Dispose();
            MemoryAddress.Free(ref tomlTable);
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            tomlTable = MemoryAddress.AllocatePointer<Implementation>();
            tomlTable->keyValues = new(4);

            TOMLReader tomlReader = new(byteReader);
            tomlReader.ReadToken(); //[
            Token nameToken = tomlReader.ReadToken();
            tomlReader.ReadToken(); //]

            Span<char> nameBuffer = stackalloc char[nameToken.length * 4];
            tomlTable->nameLength = tomlReader.GetText(nameToken, nameBuffer);
            tomlTable->name = MemoryAddress.Allocate(nameBuffer.Slice(0, tomlTable->nameLength));

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
                    tomlTable->keyValues.Add(keyValue);
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
            using Text destination = new(32);
            ToString(destination);
            byteWriter.WriteUTF8(destination.AsSpan());
        }

        public readonly bool ContainsValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(tomlTable);

            Span<TOMLKeyValue> keyValues = tomlTable->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlTable);

            Span<TOMLKeyValue> keyValues = tomlTable->keyValues.AsSpan();
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
            MemoryAddress.ThrowIfDefault(tomlTable);
            ThrowIfValueIsMissing(key);

            Span<TOMLKeyValue> keyValues = tomlTable->keyValues.AsSpan();
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

        private struct Implementation
        {
            public int nameLength;
            public MemoryAddress name;
            public List<TOMLKeyValue> keyValues;
        }
    }
}