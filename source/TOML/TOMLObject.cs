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

        public readonly TOMLKeyValue this[ReadOnlySpan<char> key]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(tomlObject);
                ThrowIfKeyIsMissing(key);

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
        }
#endif

        public TOMLObject(void* pointer)
        {
            this.tomlObject = (Implementation*)pointer;
        }

        public readonly override string ToString()
        {
            using Text destination = new(32);
            ToString(destination);
            return destination.ToString();
        }

        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(tomlObject);
            foreach (TOMLKeyValue keyValue in tomlObject->keyValues)
            {
                keyValue.ToString(destination);
            }
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(tomlObject);

            foreach (TOMLKeyValue keyValue in tomlObject->keyValues)
            {
                keyValue.Dispose();
            }

            tomlObject->keyValues.Dispose();
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

        public readonly bool ContainsKey(ReadOnlySpan<char> key)
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

        void ISerializable.Read(ByteReader byteReader)
        {
            tomlObject = MemoryAddress.AllocatePointer<Implementation>();
            tomlObject->keyValues = new(4);
            TOMLReader tomlReader = new(byteReader);
            while (tomlReader.PeekToken(out Token token))
            {
                if (token.type == Token.Type.Hash)
                {
                    //skip comments
                    tomlReader.ReadToken();
                    tomlReader.ReadToken();
                }
                else if (token.type == Token.Type.Text)
                {
                    TOMLKeyValue keyValue = byteReader.ReadObject<TOMLKeyValue>();
                    tomlObject->keyValues.Add(keyValue);
                }
                else
                {
                    tomlReader.ReadToken();
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
            Text list = new(0);
            ToString(list);
            byteWriter.WriteUTF8(list.AsSpan());
            list.Dispose();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfKeyIsMissing(ReadOnlySpan<char> key)
        {
            if (!ContainsKey(key))
            {
                throw new ArgumentException($"Key '{key.ToString()}' is missing in TOML object", nameof(key));
            }
        }

        /// <summary>
        /// Creates a new empty TOML object.
        /// </summary>
        /// <returns></returns>
        public static TOMLObject Create()
        {
            Implementation* tomlObject = MemoryAddress.AllocatePointer<Implementation>();
            tomlObject->keyValues = new(4);
            return new(tomlObject);
        }

        private struct Implementation
        {
            public List<TOMLKeyValue> keyValues;
        }
    }
}