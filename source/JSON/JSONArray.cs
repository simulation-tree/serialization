using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Serialization.JSON
{
    public unsafe struct JSONArray : IDisposable, ISerializable
    {
        private Implementation* value;

        public readonly uint Count => value->elements.Count;
        public readonly bool IsDisposed => value is null;
        public readonly nint Address => (nint)value;
        public readonly USpan<JSONProperty> Elements => value->elements.AsSpan();

        public readonly JSONProperty this[uint index]
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

        public readonly void ToString(Text result, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            ThrowIfDisposed();

            result.Append('[');
            if (value->elements.Count > 0)
            {
                NewLine();
                for (byte i = 0; i <= depth; i++)
                {
                    Indent(indent);
                }

                uint position = 0;
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

            void Indent(USpan<char> indent)
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
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= Count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
        }

        public readonly void Add(USpan<char> text)
        {
            ThrowIfDisposed();

            USpan<char> nameBuffer = stackalloc char[16];
            uint index = value->elements.Count;
            uint length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), text));
        }

        public readonly void Add(string text)
        {
            Add(text.AsSpan());
        }

        public readonly void Add(double number)
        {
            ThrowIfDisposed();

            USpan<char> nameBuffer = stackalloc char[16];
            uint index = value->elements.Count;
            uint length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), number));
        }

        public readonly void Add(bool boolean)
        {
            ThrowIfDisposed();

            USpan<char> nameBuffer = stackalloc char[16];
            uint index = value->elements.Count;
            uint length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), boolean));
        }

        public readonly void Add(JSONObject jsonObject)
        {
            ThrowIfDisposed();

            USpan<char> nameBuffer = stackalloc char[16];
            uint index = value->elements.Count;
            uint length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonObject));
        }

        public readonly void Add(JSONArray jsonArray)
        {
            ThrowIfDisposed();

            USpan<char> nameBuffer = stackalloc char[16];
            uint index = value->elements.Count;
            uint length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonArray));
        }

        public readonly void AddNull()
        {
            ThrowIfDisposed();

            USpan<char> nameBuffer = stackalloc char[16];
            uint index = value->elements.Count;
            uint length = index.ToString(nameBuffer);
            value->elements.Add(new JSONProperty(nameBuffer.Slice(0, length)));
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            Text list = new(0);
            ToString(list);
            writer.WriteUTF8Text(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = Implementation.Allocate();
            ParseArray(new(reader), reader, this);
            static void ParseArray(JSONReader jsonReader, BinaryReader reader, JSONArray jsonArray)
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
                        USpan<char> bufferSpan = textBuffer.AsSpan();
                        uint textLength = jsonReader.GetText(token, bufferSpan);
                        USpan<char> text = bufferSpan.Slice(0, textLength);
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
                Implementation* obj = Allocations.Allocate<Implementation>();
                obj[0] = new(elements);
                return obj;
            }

            public static void Free(ref Implementation* array)
            {
                Allocations.ThrowIfNull(array);

                for (uint i = 0; i < array->elements.Count; i++)
                {
                    JSONProperty property = array->elements[i];
                    property.Dispose();
                }

                array->elements.Dispose();
                Allocations.Free(ref array);
            }
        }
    }
}