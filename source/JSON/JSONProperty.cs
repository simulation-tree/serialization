using System;

namespace Unmanaged.JSON
{
    public struct JSONProperty : IDisposable
    {
        private readonly Text name;
        private Allocation value;
        private uint length;
        private Type type;

        public readonly bool IsText => type == Type.Text;
        public readonly bool IsNumber => type == Type.Number;
        public readonly bool IsBoolean => type == Type.Boolean;
        public readonly bool IsObject => type == Type.Object;
        public readonly bool IsArray => type == Type.Array;
        public readonly bool IsNull => type == Type.Null;
        public readonly Type PropertyType => type;
        public readonly USpan<char> Name => name.AsSpan();
        public readonly bool IsDisposed => type == default || name.IsDisposed;

        public USpan<char> Text
        {
            readonly get
            {
                if (IsText)
                {
                    return value.AsSpan<char>(0, length / sizeof(char));
                }
                else
                {
                    throw new InvalidOperationException($"Property is not of type {Type.Text}");
                }
            }
            set
            {
                if (IsText)
                {
                    uint newLength = (uint)value.Length * sizeof(char);
                    if (length < newLength)
                    {
                        Allocation.Resize(ref this.value, newLength);
                    }

                    length = newLength;
                    this.value.Write(0, value);
                }
                else
                {
                    throw new InvalidOperationException($"Property is not of type {Type.Text}");
                }
            }
        }

        public readonly ref double Number
        {
            get
            {
                return ref value.Read<double>();
            }
        }

        public readonly ref bool Boolean
        {
            get
            {
                return ref value.Read<bool>();
            }
        }

        public readonly unsafe JSONObject Object
        {
            get
            {
                return value.Read<JSONObject>();
            }
            set
            {
                if (IsObject)
                {
                    this.value.Read<JSONObject>().Dispose();
                    this.value.Write(0, value);
                }
                else
                {
                    throw new InvalidOperationException($"Property is not of type {Type.Object}");
                }
            }
        }

        public readonly unsafe JSONArray Array
        {
            get
            {
                return value.Read<JSONArray>();
            }
            set
            {
                if (IsArray)
                {
                    this.value.Read<JSONArray>().Dispose();
                    this.value.Write(0, value);
                }
                else
                {
                    throw new InvalidOperationException($"Property is not of type {Type.Array}");
                }
            }
        }

        public JSONProperty(USpan<char> name, USpan<char> text)
        {
            this.name = new(name);
            length = text.Length * sizeof(char);
            value = new(length);
            value.Write(0, text);
            type = Type.Text;
        }

        public JSONProperty(USpan<char> name, double number)
        {
            this.name = new(name);
            length = sizeof(double);
            value = new(length);
            value.Write(0, number);
            type = Type.Number;
        }

        public JSONProperty(USpan<char> name, bool boolean)
        {
            this.name = new(name);
            length = sizeof(bool);
            value = new(length);
            value.Write(0, boolean);
            type = Type.Boolean;
        }

        public unsafe JSONProperty(USpan<char> name, JSONObject obj)
        {
            this.name = new(name);
            length = (uint)sizeof(nint);
            value = new(length);
            value.Write(0, obj.Address);
            type = Type.Object;
        }

        public unsafe JSONProperty(USpan<char> name, JSONArray array)
        {
            this.name = new(name);
            length = (uint)sizeof(nint);
            value = new(length);
            value.Write(0, array.Address);
            type = Type.Array;
        }

        public JSONProperty(USpan<char> name)
        {
            this.name = new(name);
            length = 0;
            value = default;
            type = Type.Null;
        }

        public unsafe void Dispose()
        {
            if (type == Type.Object)
            {
                nint address = value.Read<nint>();
                JSONObject jsonObject = new((void*)address);
                jsonObject.Dispose();
            }
            else if (type == Type.Array)
            {
                nint address = value.Read<nint>();
                JSONArray jsonArray = new((void*)address);
                jsonArray.Dispose();
            }

            name.Dispose();
            value.Dispose();
            type = default;
        }

        public unsafe readonly void ToString(Text result, bool prefixName, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            if (prefixName)
            {
                result.Append('\"');
                result.Append(Name);
                result.Append('\"');
                result.Append(':');
            }

            if (type == Type.Text)
            {
                result.Append('\"');
                result.Append(Text);
                result.Append('\"');
            }
            else if (type == Type.Number)
            {
                double number = Number;
                USpan<char> buffer = stackalloc char[64];
                uint length = number.ToString(buffer);
                result.Append(buffer.Slice(0, length));
            }
            else if (type == Type.Boolean)
            {
                result.Append(Boolean ? "true".AsUSpan() : "false".AsUSpan());
            }
            else if (type == Type.Object)
            {
                void* ptr = (void*)value.Read<nint>();
                JSONObject obj = new(ptr);
                obj.ToString(result, indent, cr, lf, depth);
            }
            else if (type == Type.Array)
            {
                void* ptr = (void*)value.Read<nint>();
                JSONArray array = new(ptr);
                array.ToString(result, indent, cr, lf, depth);
            }
            else if (type == Type.Null)
            {
                result.Append('n');
                result.Append('u');
                result.Append('l');
                result.Append('l');
            }
            else
            {
                throw new InvalidOperationException($"Property is of an unknown type: {type}");
            }
        }

        public readonly override string ToString()
        {
            Text buffer = new(0);
            ToString(buffer, true);
            string result = buffer.ToString();
            buffer.Dispose();
            return result;
        }

        public readonly bool TryGetText(out USpan<char> text)
        {
            if (IsText)
            {
                text = Text;
                return true;
            }

            text = default;
            return false;
        }

        public readonly bool TryGetNumber(out double number)
        {
            if (IsNumber)
            {
                number = Number;
                return true;
            }

            number = default;
            return false;
        }

        public readonly bool TryGetBoolean(out bool boolean)
        {
            if (IsBoolean)
            {
                boolean = value.Read<bool>();
                return true;
            }

            boolean = default;
            return false;
        }

        public unsafe readonly bool TryGetObject(out JSONObject obj)
        {
            if (IsObject)
            {
                obj = Object;
                return true;
            }

            obj = default;
            return false;
        }

        public unsafe readonly bool TryGetArray(out JSONArray array)
        {
            if (IsArray)
            {
                array = Array;
                return true;
            }

            array = default;
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