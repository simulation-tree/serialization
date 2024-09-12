using System;
using System.Diagnostics;
using Unmanaged.Collections;
using Unmanaged.JSON.Array;

namespace Unmanaged.JSON
{
    public unsafe struct JSONArray : IDisposable, ISerializable
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

#if NET
        public JSONArray()
        {
            value = UnsafeJSONArray.Allocate();
        }
#endif

        private JSONArray(UnsafeJSONArray* value)
        {
            this.value = value;
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

        public readonly void ToString(UnmanagedList<char> result, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
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
                    ref JSONProperty element = ref elements[position];
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

            void Indent(USpan<char> indent)
            {
                result.AddRange(indent);
            }
        }

        public readonly override string ToString()
        {
            UnmanagedList<char> result = UnmanagedList<char>.Create();
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

        public readonly void Add(USpan<char> text)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            USpan<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            uint length = index.ToString(nameBuffer);
            elements.Add(new JSONProperty(nameBuffer.Slice(0, length), text));
        }

        public readonly void Add(string text)
        {
            Add(text.AsUSpan());
        }

        public readonly void Add(double number)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            USpan<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            uint length = index.ToString(nameBuffer);
            elements.Add(new JSONProperty(nameBuffer.Slice(0, length), number));
        }

        public readonly void Add(bool boolean)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            USpan<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            uint length = index.ToString(nameBuffer);
            elements.Add(new JSONProperty(nameBuffer.Slice(0, length), boolean));
        }

        public readonly void Add(JSONObject jsonObject)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            USpan<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            uint length = index.ToString(nameBuffer);
            elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonObject));
        }

        public readonly void Add(JSONArray jsonArray)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            USpan<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            uint length = index.ToString(nameBuffer);
            elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonArray));
        }

        public readonly void AddNull()
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> elements = Elements;
            USpan<char> nameBuffer = stackalloc char[16];
            uint index = elements.Count;
            uint length = index.ToString(nameBuffer);
            elements.Add(new JSONProperty(nameBuffer.Slice(0, length)));
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            UnmanagedList<char> list = UnmanagedList<char>.Create();
            ToString(list);
            writer.WriteUTF8Text(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = UnsafeJSONArray.Allocate();
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
                        UnmanagedArray<char> listBuffer = new(token.length * 4);
                        USpan<char> textSpan = listBuffer.AsSpan();
                        uint textLength = jsonReader.GetText(token, textSpan);
                        USpan<char> text = textSpan.Slice(0, textLength);
                        if (text.length > 0 && text[0] == '"')
                        {
                            text = text.Slice(1, text.length - 2);
                        }

                        jsonArray.Add(text);
                        listBuffer.Dispose();
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
            return new(UnsafeJSONArray.Allocate());
        }
    }
}