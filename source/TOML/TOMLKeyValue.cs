using System;
using System.Diagnostics;
using Unmanaged;

namespace Serialization.TOML
{
    public unsafe struct TOMLKeyValue : IDisposable, ISerializable
    {
        private Implementation* keyValue;

        public readonly ReadOnlySpan<char> Key
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);

                return keyValue->key.GetSpan<char>(keyValue->keyLength);
            }
        }

        public readonly ValueType ValueType
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);

                return keyValue->valueType;
            }
        }

        public readonly bool IsDisposed => keyValue == default;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public TOMLKeyValue()
        {
        }
#endif

        public TOMLKeyValue(ReadOnlySpan<char> key, ReadOnlySpan<char> text)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Text;
            keyValue->keyLength = key.Length;
            keyValue->key = MemoryAddress.Allocate(key);
            keyValue->valueLength = text.Length;
            keyValue->value = MemoryAddress.Allocate(text);
        }

        public TOMLKeyValue(ReadOnlySpan<char> key, double number)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Number;
            keyValue->keyLength = key.Length;
            keyValue->key = MemoryAddress.Allocate(key);
            keyValue->valueLength = sizeof(double);
            keyValue->value = MemoryAddress.AllocateValue(number);
        }

        public TOMLKeyValue(ReadOnlySpan<char> key, bool boolean)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Boolean;
            keyValue->keyLength = key.Length;
            keyValue->key = MemoryAddress.Allocate(key);
            keyValue->valueLength = sizeof(bool);
            keyValue->value = MemoryAddress.AllocateValue(boolean);
        }

        public readonly override string ToString()
        {
            using Text destination = new(32);
            ToString(destination);
            return destination.ToString();
        }

        public readonly void ToString(Text destination)
        {
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(keyValue);

            keyValue->key.Dispose();
            keyValue->value.Dispose();
            MemoryAddress.Free(ref keyValue);
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            TOMLReader tomlReader = new(byteReader);
            Token token = tomlReader.ReadToken();
            Span<char> buffer = stackalloc char[token.length * 4];
            keyValue->keyLength = tomlReader.GetText(token, buffer);
            keyValue->key = MemoryAddress.Allocate(buffer.Slice(0, keyValue->keyLength));

            token = tomlReader.ReadToken();
            ThrowIfNotEqualsAfterKey(token.type);

            token = tomlReader.ReadToken();
            buffer = stackalloc char[token.length * 4];
            keyValue->valueLength = tomlReader.GetText(token, buffer);
            ReadOnlySpan<char> valueText = buffer.Slice(0, keyValue->valueLength);
            if (double.TryParse(valueText, out double number))
            {
                keyValue->valueType = ValueType.Number;
                keyValue->value = MemoryAddress.AllocateValue(number);
            }
            else if (bool.TryParse(valueText, out bool boolean))
            {
                keyValue->valueType = ValueType.Boolean;
                keyValue->value = MemoryAddress.AllocateValue(boolean);
            }
            else
            {
                keyValue->valueType = ValueType.Text;
                keyValue->value = MemoryAddress.Allocate(valueText);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotEqualsAfterKey(Token.Type type)
        {
            if (type != Token.Type.Equals)
            {
                throw new InvalidOperationException($"Expected '=' after key, but got '{type}'");
            }
        }

        private struct Implementation
        {
            public ValueType valueType;
            public int keyLength;
            public MemoryAddress key;
            public int valueLength;
            public MemoryAddress value;
        }
    }
}