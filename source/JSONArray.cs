using System;
using System.Diagnostics;
using Unmanaged.Collections;
using Unmanaged.JSON.Array;

namespace Unmanaged.JSON
{
    public unsafe struct JSONArray : IDisposable
    {
        private UnsafeJSONArray* value;

        public readonly uint Count => UnsafeJSONArray.GetCount(value);
        public readonly bool IsDisposed => UnsafeJSONArray.IsDisposed(value);
        public readonly nint Address => (nint)value;

        private readonly UnmanagedList<JSONProperty> Elements => UnsafeJSONArray.GetElements(value);

        public readonly JSONProperty this[uint index]
        {
            get
            {
                ThrowIfDisposed();
                UnmanagedList<JSONProperty> elements = Elements;
                if (index >= elements.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return elements[index];
            }
        }

        public JSONArray()
        {
            value = UnsafeJSONArray.Allocate();
        }

        public JSONArray(void* value)
        {
            this.value = (UnsafeJSONArray*)value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeJSONArray.Free(ref value);
        }

        public readonly void ParseFrom(ref JSONReader reader)
        {
            ThrowIfDisposed();
            ParseArray(ref reader, this);
            static void ParseArray(ref JSONReader reader, JSONArray jsonArray)
            {
                while (reader.ReadToken(out JSONToken token))
                {
                    if (token.type == JSONToken.Type.True)
                    {
                        jsonArray.Add(reader.GetBoolean(token));
                    }
                    else if (token.type == JSONToken.Type.False)
                    {
                        jsonArray.Add(reader.GetBoolean(token));
                    }
                    else if (token.type == JSONToken.Type.Null)
                    {
                        jsonArray.AddNull();
                    }
                    else if (token.type == JSONToken.Type.Number)
                    {
                        jsonArray.Add(reader.GetNumber(token));
                    }
                    else if (token.type == JSONToken.Type.Text)
                    {
                        jsonArray.Add(reader.GetText(token));
                    }
                    else if (token.type == JSONToken.Type.StartObject)
                    {
                        JSONObject newObject = new();
                        newObject.ParseFrom(ref reader);
                        jsonArray.Add(newObject);
                    }
                    else if (token.type == JSONToken.Type.StartArray)
                    {
                        JSONArray newArray = new();
                        newArray.ParseFrom(ref reader);
                        jsonArray.Add(newArray);
                    }
                    else if (token.type == JSONToken.Type.EndArray)
                    {
                        break;
                    }
                }
            }
        }

        public readonly void ParseFrom(ReadOnlySpan<byte> jsonBytes)
        {
            ThrowIfDisposed();
            JSONReader reader = new(jsonBytes);
            ParseFrom(ref reader);
            reader.Dispose();
        }

        public readonly void ToString(UnmanagedList<char> result, ReadOnlySpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            result.Add('[');
            if (elements.Count > 0)
            {
                NewLine();
                for (byte i = 0; i <= depth; i++)
                {
                    Indent(indent);
                }

                uint position = 0;
                while (true)
                {
                    ref JSONProperty element = ref elements.GetRef(position);
                    byte childDepth = depth;
                    childDepth++;
                    element.ToString(result, false, indent, cr, lf, childDepth);
                    position++;

                    if (position == Count)
                    {
                        break;
                    }

                    result.Add(',');
                    NewLine();
                    for (byte i = 0; i <= depth; i++)
                    {
                        Indent(indent);
                    }
                }

                NewLine();
                for (byte i = 0; i < depth; i++)
                {
                    Indent(indent);
                }
            }

            result.Add(']');

            void NewLine()
            {
                if (cr)
                {
                    result.Add('\r');
                }

                if (lf)
                {
                    result.Add('\n');
                }
            }

            void Indent(ReadOnlySpan<char> indent)
            {
                result.AddRange(indent);
            }
        }

        public readonly override string ToString()
        {
            UnmanagedList<char> result = new();
            ToString(result);
            string text = result.AsSpan().ToString();
            result.Dispose();
            return text;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(JSONArray));
            }
        }

        public readonly void Add(ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> elements = Elements;
            Span<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            index.TryFormat(nameBuffer, out int charsWritten);
            elements.Add(new JSONProperty(nameBuffer[..charsWritten], text));
        }

        public readonly void Add(double number)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> elements = Elements;
            Span<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            index.TryFormat(nameBuffer, out int charsWritten);
            elements.Add(new JSONProperty(nameBuffer[..charsWritten], number));
        }

        public readonly void Add(bool boolean)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> elements = Elements;
            Span<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            index.TryFormat(nameBuffer, out int charsWritten);
            elements.Add(new JSONProperty(nameBuffer[..charsWritten], boolean));
        }

        public readonly void Add(JSONObject jsonObject)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> elements = Elements;
            Span<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            index.TryFormat(nameBuffer, out int charsWritten);
            elements.Add(new JSONProperty(nameBuffer[..charsWritten], jsonObject));
        }

        public readonly void Add(JSONArray jsonArray)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> elements = Elements;
            Span<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            index.TryFormat(nameBuffer, out int charsWritten);
            elements.Add(new JSONProperty(nameBuffer[..charsWritten], jsonArray));
        }

        public readonly void AddNull()
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> elements = Elements;
            Span<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            index.TryFormat(nameBuffer, out int charsWritten);
            elements.Add(new JSONProperty(nameBuffer[..charsWritten]));
        }
    }
}