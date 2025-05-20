using System;
using System.Diagnostics;
using System.Globalization;
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

                return keyValue->data.GetSpan<char>(keyValue->keyLength);
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

        public readonly ReadOnlySpan<char> Text
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Text);

                return keyValue->data.AsSpan<char>(keyValue->keyLength, keyValue->valueLength);
            }
        }

        public readonly ref double Number
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Number);

                return ref keyValue->data.Read<double>(keyValue->keyLength * sizeof(char));
            }
        }

        public readonly ref bool Boolean
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Boolean);

                return ref keyValue->data.Read<bool>(keyValue->keyLength * sizeof(char));
            }
        }

        public readonly ref DateTime DateTime
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.DateTime);

                return ref keyValue->data.Read<DateTime>(keyValue->keyLength * sizeof(char));
            }
        }

        public readonly ref TimeSpan TimeSpan
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.TimeSpan);

                return ref keyValue->data.Read<TimeSpan>(keyValue->keyLength * sizeof(char));
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
            keyValue->valueLength = text.Length;

            int keyByteLength = sizeof(char) * key.Length;
            int textByteLength = sizeof(char) * text.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + textByteLength);
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.CopyFrom(text, keyByteLength);
        }

        public TOMLKeyValue(ReadOnlySpan<char> key, double number)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Number;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(double));
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, number);
        }

        public TOMLKeyValue(ReadOnlySpan<char> key, bool boolean)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Boolean;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + 1);
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, boolean);
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
            MemoryAddress.ThrowIfDefault(keyValue);

            keyValue->data.Dispose();
            MemoryAddress.Free(ref keyValue);
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            TOMLReader tomlReader = new(byteReader);

            //read text
            Token keyToken = tomlReader.ReadToken();
            Span<char> keyBuffer = stackalloc char[keyToken.length * 4];
            keyValue->keyLength = tomlReader.GetText(keyToken, keyBuffer);
            ReadOnlySpan<char> keyText = keyBuffer.Slice(0, keyValue->keyLength);

            //read equals
            Token equalsToken = tomlReader.ReadToken();
            ThrowIfNotEqualsAfterKey(equalsToken.type);

            //read text
            Token valueToken = tomlReader.ReadToken();
            Span<char> valueBuffer = stackalloc char[valueToken.length * 4];
            int valueLength = tomlReader.GetText(valueToken, valueBuffer);
            ReadOnlySpan<char> valueText = valueBuffer.Slice(0, valueLength);

            //build data
            int keyByteLength = sizeof(char) * keyValue->keyLength;
            if (TimeSpan.TryParse(valueText, out TimeSpan timeSpan))
            {
                keyValue->valueType = ValueType.TimeSpan;
                keyValue->valueLength = 1;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(TimeSpan));
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.Write(keyByteLength, timeSpan);
            }
            else if (DateTimeOffset.TryParse(valueText, default, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset dateTimeOffset))
            {
                keyValue->valueType = ValueType.DateTime;
                keyValue->valueLength = 1;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(DateTime));
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.Write(keyByteLength, dateTimeOffset.DateTime);
            }
            else if (DateTime.TryParse(valueText, out DateTime dateTime))
            {
                keyValue->valueType = ValueType.DateTime;
                keyValue->valueLength = 1;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(DateTime));
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.Write(keyByteLength, dateTime);
            }
            else if (double.TryParse(valueText, out double number))
            {
                keyValue->valueType = ValueType.Number;
                keyValue->valueLength = 1;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(double));
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.Write(keyByteLength, number);
            }
            else if (bool.TryParse(valueText, out bool boolean))
            {
                keyValue->valueType = ValueType.Boolean;
                keyValue->valueLength = 1;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + 1);
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.Write(keyByteLength, boolean);
            }
            else
            {
                keyValue->valueType = ValueType.Text;
                keyValue->valueLength = valueLength;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + (sizeof(char) * valueLength));
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.CopyFrom(valueText, keyByteLength);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotTypeOf(ValueType type)
        {
            if (keyValue->valueType != type)
            {
                throw new InvalidOperationException($"Expected value type `{type}`, but got `{keyValue->valueType}`");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotEqualsAfterKey(Token.Type type)
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
            public int valueLength;
            public MemoryAddress data;
        }
    }
}