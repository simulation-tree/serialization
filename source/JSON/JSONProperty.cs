using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.JSON
{
    [SkipLocalsInit]
    public struct JSONProperty : IDisposable
    {
        private readonly Text name;
        private MemoryAddress value;
        private int length;
        private Type type;

        public readonly bool IsText
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Text;
            }
        }

        public readonly bool IsNumber
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Number;
            }
        }

        public readonly bool IsBoolean
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Boolean;
            }
        }

        public readonly bool IsObject
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Object;
            }
        }

        public readonly bool IsArray
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Array;
            }
        }

        public readonly bool IsNull
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Null;
            }
        }

        public readonly Type PropertyType
        {
            get
            {
                ThrowIfDisposed();

                return type;
            }
        }

        public readonly ReadOnlySpan<char> Name
        {
            get
            {
                ThrowIfDisposed();

                return name.AsSpan();
            }
        }

        public readonly bool IsDisposed => type == default || name.IsDisposed;

        public ReadOnlySpan<char> Text
        {
            readonly get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Text);

                return value.GetSpan<char>(length / sizeof(char));
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Text);

                int newLength = value.Length * sizeof(char);
                if (length < newLength)
                {
                    MemoryAddress.Resize(ref this.value, newLength);
                }

                length = newLength;
                this.value.Write(0, value);
            }
        }

        public readonly ref double Number
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Number);

                return ref value.Read<double>();
            }
        }

        public readonly ref bool Boolean
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Boolean);

                return ref value.Read<bool>();
            }
        }

        public readonly JSONObject Object
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Object);

                return value.Read<JSONObject>();
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Object);

                this.value.Read<JSONObject>().Dispose();
                this.value.Write(0, value);
            }
        }

        public readonly JSONArray Array
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Array);

                return value.Read<JSONArray>();
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Array);

                this.value.Read<JSONArray>().Dispose();
                this.value.Write(0, value);
            }
        }

        public JSONProperty(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            this.name = new(name);
            length = text.Length * sizeof(char);
            value = MemoryAddress.Allocate(length);
            value.Write(0, text);
            type = Type.Text;
        }

        public JSONProperty(ReadOnlySpan<char> name, double number)
        {
            this.name = new(name);
            value = MemoryAddress.AllocateValue(number, out length);
            type = Type.Number;
        }

        public JSONProperty(ReadOnlySpan<char> name, bool boolean)
        {
            this.name = new(name);
            value = MemoryAddress.AllocateValue(boolean, out length);
            type = Type.Boolean;
        }

        public JSONProperty(ReadOnlySpan<char> name, JSONObject jsonObject)
        {
            this.name = new(name);
            value = MemoryAddress.AllocateValue(jsonObject, out length);
            type = Type.Object;
        }

        public JSONProperty(ReadOnlySpan<char> name, JSONArray jsonArray)
        {
            this.name = new(name);
            value = MemoryAddress.AllocateValue(jsonArray, out length);
            type = Type.Array;
        }

        /// <summary>
        /// Creates a null property.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name)
        {
            this.name = new(name);
            length = 0;
            value = default;
            type = Type.Null;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (type == default)
            {
                throw new ObjectDisposedException(nameof(JSONProperty), "The JSON property has been disposed");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTypeMismatch(Type desiredType)
        {
            if (type != desiredType)
            {
                throw new InvalidOperationException($"Property is not of type {desiredType}");
            }
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            if (type == Type.Object)
            {
                JSONObject jsonObject = value.Read<JSONObject>();
                jsonObject.Dispose();
            }
            else if (type == Type.Array)
            {
                JSONArray jsonArray = value.Read<JSONArray>();
                jsonArray.Dispose();
            }

            name.Dispose();
            value.Dispose();
            type = default;
        }

        public readonly void ToString(Text result, SerializationSettings settings = default)
        {
            ToString(result, settings, 0);
        }

        internal readonly void ToString(Text result, SerializationSettings settings, byte depth)
        {
            ThrowIfDisposed();

            if (type == Type.Text)
            {
                result.Append('\"');
                result.Append(Text);
                result.Append('\"');
            }
            else if (type == Type.Number)
            {
                double number = value.Read<double>();
                Span<char> buffer = stackalloc char[64];
                int length = number.ToString(buffer);
                result.Append(buffer.Slice(0, length));
            }
            else if (type == Type.Boolean)
            {
                result.Append(value.Read<bool>() ? Token.True : Token.False);
            }
            else if (type == Type.Object)
            {
                JSONObject jsonObject = value.Read<JSONObject>();
                jsonObject.ToString(result, settings, depth);
            }
            else if (type == Type.Array)
            {
                JSONArray jsonArray = value.Read<JSONArray>();
                jsonArray.ToString(result, settings, depth);
            }
            else if (type == Type.Null)
            {
                result.Append(Token.Null);
            }
            else
            {
                throw new InvalidOperationException($"Property is of an unknown type: {type}");
            }
        }

        public readonly override string ToString()
        {
            ThrowIfDisposed();

            if (type == Type.Text)
            {
                return Text.ToString();
            }
            else if (type == Type.Number)
            {
                double number = Number;
                Span<char> buffer = stackalloc char[64];
                int length = number.ToString(buffer);
                return buffer.Slice(0, length).ToString();
            }
            else if (type == Type.Boolean)
            {
                return Boolean ? Token.True : Token.False;
            }
            else if (type == Type.Object)
            {
                JSONObject jsonObject = value.Read<JSONObject>();
                return jsonObject.ToString();
            }
            else if (type == Type.Array)
            {
                JSONArray jsonArray = value.Read<JSONArray>();
                return jsonArray.ToString();
            }
            else if (type == Type.Null)
            {
                return Token.Null;
            }
            else
            {
                throw new InvalidOperationException($"Property is of an unknown type: {type}");
            }
        }

        public readonly bool TryGetText(out ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            if (type == Type.Text)
            {
                text = Text;
                return true;
            }

            text = default;
            return false;
        }

        public readonly bool TryGetNumber(out double number)
        {
            ThrowIfDisposed();

            if (type == Type.Number)
            {
                number = value.Read<double>();
                return true;
            }

            number = default;
            return false;
        }

        public readonly bool TryGetBoolean(out bool boolean)
        {
            ThrowIfDisposed();

            if (type == Type.Boolean)
            {
                boolean = value.Read<bool>();
                return true;
            }

            boolean = default;
            return false;
        }

        public readonly bool TryGetObject(out JSONObject jsonObject)
        {
            ThrowIfDisposed();

            if (type == Type.Object)
            {
                jsonObject = value.Read<JSONObject>();
                return true;
            }

            jsonObject = default;
            return false;
        }

        public readonly bool TryGetArray(out JSONArray jsonArray)
        {
            ThrowIfDisposed();

            if (type == Type.Array)
            {
                jsonArray = value.Read<JSONArray>();
                return true;
            }

            jsonArray = default;
            return false;
        }

        public enum Type : byte
        {
            Unknown = 0,
            Text = 1,
            Number = 2,
            Boolean = 3,
            Object = 4,
            Array = 5,
            Null = 6,
        }
    }
}