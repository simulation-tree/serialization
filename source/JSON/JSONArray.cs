using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.JSON
{
    [SkipLocalsInit]
    public unsafe struct JSONArray : IDisposable, ISerializable
    {
        private Implementation* value;

        public readonly int Count => value->elements.Count;
        public readonly bool IsDisposed => value is null;
        public readonly nint Address => (nint)value;
        public readonly ReadOnlySpan<JSONProperty> Elements => value->elements.AsSpan();

        public readonly JSONProperty this[int index]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfOutOfRange(index);

                return value->elements[index];
            }
        }

#if NET
        public JSONArray()
        {
            value = Implementation.Allocate();
        }
#endif

        public JSONArray(void* value)
        {
            this.value = (Implementation*)value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            Implementation.Free(ref value);
        }

        public readonly void ToString(Text result, ReadOnlySpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            ThrowIfDisposed();

            result.Append('[');
            if (value->elements.Count > 0)
            {
                NewLine();
                for (int i = 0; i <= depth; i++)
                {
                    Indent(indent);
                }

                int position = 0;
                while (true)
                {
                    ref JSONProperty element = ref value->elements[position];
                    byte childDepth = depth;
                    childDepth++;
                    element.ToString(result, false, indent, cr, lf, childDepth);
                    position++;

                    if (position == Count)
                    {
                        break;
                    }

                    result.Append(',');
                    NewLine();
                    for (int i = 0; i <= depth; i++)
                    {
                        Indent(indent);
                    }
                }

                NewLine();
                for (int i = 0; i < depth; i++)
                {
                    Indent(indent);
                }
            }

            result.Append(']');

            void NewLine()
            {
                if (cr)
                {
                    result.Append('\r');
                }

                if (lf)
                {
                    result.Append('\n');
                }
            }

            void Indent(ReadOnlySpan<char> indent)
            {
                result.Append(indent);
            }
        }

        public readonly override string ToString()
        {
            Text result = new(0);
            ToString(result);
            string text = result.ToString();
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

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= Count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
        }

        public readonly void Add(ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = value->elements.Count;
            int length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), text));
        }

        public readonly void Add(string text)
        {
            Add(text.AsSpan());
        }

        public readonly void Add(double number)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = value->elements.Count;
            int length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), number));
        }

        public readonly void Add(bool boolean)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = value->elements.Count;
            int length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), boolean));
        }

        public readonly void Add(JSONObject jsonObject)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = value->elements.Count;
            int length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonObject));
        }

        public readonly void Add(JSONArray jsonArray)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = value->elements.Count;
            int length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonArray));
        }

        public readonly void AddNull()
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = value->elements.Count;
            int length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length)));
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            Text list = new(0);
            ToString(list);
            writer.WriteUTF8(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(ByteReader reader)
        {
            value = Implementation.Allocate();
            ParseArray(new(reader), reader, this);
            static void ParseArray(JSONReader jsonReader, ByteReader reader, JSONArray jsonArray)
            {
                while (jsonReader.ReadToken(out Token token))
                {
                    if (token.type == Token.Type.True)
                    {
                        jsonArray.Add(jsonReader.GetBoolean(token));
                    }
                    else if (token.type == Token.Type.False)
                    {
                        jsonArray.Add(jsonReader.GetBoolean(token));
                    }
                    else if (token.type == Token.Type.Null)
                    {
                        jsonArray.AddNull();
                    }
                    else if (token.type == Token.Type.Number)
                    {
                        jsonArray.Add(jsonReader.GetNumber(token));
                    }
                    else if (token.type == Token.Type.Text)
                    {
                        Text textBuffer = new(token.length * 4);
                        Span<char> bufferSpan = textBuffer.AsSpan();
                        int textLength = jsonReader.GetText(token, bufferSpan);
                        Span<char> text = bufferSpan.Slice(0, textLength);
                        if (text.Length > 0 && text[0] == '"')
                        {
                            text = text.Slice(1, text.Length - 2);
                        }

                        jsonArray.Add(text);
                        textBuffer.Dispose();
                    }
                    else if (token.type == Token.Type.StartObject)
                    {
                        JSONObject newObject = reader.ReadObject<JSONObject>();
                        jsonArray.Add(newObject);
                    }
                    else if (token.type == Token.Type.StartArray)
                    {
                        JSONArray newArray = reader.ReadObject<JSONArray>();
                        jsonArray.Add(newArray);
                    }
                    else if (token.type == Token.Type.EndArray)
                    {
                        break;
                    }
                }
            }
        }

        public static JSONArray Create()
        {
            return new(Implementation.Allocate());
        }

        public readonly struct Implementation
        {
            public readonly List<JSONProperty> elements;

            private Implementation(List<JSONProperty> elements)
            {
                this.elements = elements;
            }

            public static Implementation* Allocate()
            {
                List<JSONProperty> elements = new(4);
                ref Implementation value = ref MemoryAddress.Allocate<Implementation>();
                value = new(elements);
                fixed (Implementation* pointer = &value)
                {
                    return pointer;
                }
            }

            public static void Free(ref Implementation* array)
            {
                MemoryAddress.ThrowIfDefault(array);

                for (int i = 0; i < array->elements.Count; i++)
                {
                    JSONProperty property = array->elements[i];
                    property.Dispose();
                }

                array->elements.Dispose();
                MemoryAddress.Free(ref array);
            }
        }
    }
}