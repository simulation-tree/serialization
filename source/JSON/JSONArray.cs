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
        private Implementation* jsonArray;

        public readonly int Count
        {
            get
            {
                ThrowIfDisposed();

                return jsonArray->elements.Count;
            }
        }

        public readonly bool IsDisposed => jsonArray is null;
        public readonly nint Address => (nint)jsonArray;

        public readonly ReadOnlySpan<JSONProperty> Elements
        {
            get
            {
                ThrowIfDisposed();

                return jsonArray->elements.AsSpan();
            }
        }

        public readonly JSONProperty this[int index]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfOutOfRange(index);

                return jsonArray->elements[index];
            }
        }

#if NET
        public JSONArray()
        {
            jsonArray = MemoryAddress.AllocatePointer<Implementation>();
            jsonArray->elements = new(4);
        }
#endif

        public JSONArray(void* value)
        {
            this.jsonArray = (Implementation*)value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            Span<JSONProperty> elements = jsonArray->elements.AsSpan();
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].Dispose();
            }

            jsonArray->elements.Dispose();
            MemoryAddress.Free(ref jsonArray);
        }

        public readonly void ToString(Text result, SerializationSettings settings = default)
        {
            ToString(result, settings, 0);
        }

        internal readonly void ToString(Text result, SerializationSettings settings, byte depth)
        {
            ThrowIfDisposed();

            result.Append('[');
            if (jsonArray->elements.Count > 0)
            {
                settings.NewLine(result);
                for (int i = 0; i <= depth; i++)
                {
                    settings.Indent(result);
                }

                int position = 0;
                while (true)
                {
                    ref JSONProperty element = ref jsonArray->elements[position];
                    byte childDepth = depth;
                    childDepth++;
                    element.ToString(result, settings, childDepth);
                    position++;

                    if (position == Count)
                    {
                        break;
                    }

                    result.Append(',');
                    settings.NewLine(result);
                    for (int i = 0; i <= depth; i++)
                    {
                        settings.Indent(result);
                    }
                }

                settings.NewLine(result);
                for (int i = 0; i < depth; i++)
                {
                    settings.Indent(result);
                }
            }

            result.Append(']');
        }

        public readonly override string ToString()
        {
            ThrowIfDisposed();

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
            if (index >= Count || index < 0)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
        }

        public readonly void Add(ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = jsonArray->elements.Count;
            int length = index.ToString(nameBuffer);
            jsonArray->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), text));
        }

        public readonly void Add(string text)
        {
            Add(text.AsSpan());
        }

        public readonly void Add(double number)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = jsonArray->elements.Count;
            int length = index.ToString(nameBuffer);
            jsonArray->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), number));
        }

        public readonly void Add(bool boolean)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = jsonArray->elements.Count;
            int length = index.ToString(nameBuffer);
            jsonArray->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), boolean));
        }

        public readonly void Add(JSONObject jsonObject)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = jsonArray->elements.Count;
            int length = index.ToString(nameBuffer);
            jsonArray->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonObject));
        }

        public readonly void Add(JSONArray jsonArray)
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = this.jsonArray->elements.Count;
            int length = index.ToString(nameBuffer);
            this.jsonArray->elements.Add(new JSONProperty(nameBuffer.Slice(0, length), jsonArray));
        }

        public readonly void AddNull()
        {
            ThrowIfDisposed();

            Span<char> nameBuffer = stackalloc char[16];
            int index = jsonArray->elements.Count;
            int length = index.ToString(nameBuffer);
            jsonArray->elements.Add(new JSONProperty(nameBuffer.Slice(0, length)));
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            ThrowIfDisposed();

            Text list = new(0);
            ToString(list);
            writer.WriteUTF8(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(ByteReader reader)
        {
            jsonArray = MemoryAddress.AllocatePointer<Implementation>();
            jsonArray->elements = new(4);
            using Text textBuffer = new(256);
            JSONReader jsonReader = new(reader);
            while (jsonReader.ReadToken(out Token token))
            {
                if (token.type == Token.Type.Text)
                {
                    int capacity = token.length * 4;
                    if (textBuffer.Length < capacity)
                    {
                        textBuffer.SetLength(capacity);
                    }

                    int textLength = jsonReader.GetText(token, textBuffer.AsSpan());
                    Span<char> text = textBuffer.Slice(0, textLength);
                    if (double.TryParse(text, out double number))
                    {
                        Add(number);
                    }
                    else if (text.SequenceEqual(Token.True))
                    {
                        Add(true);
                    }
                    else if (text.SequenceEqual(Token.False))
                    {
                        Add(false);
                    }
                    else if (text.SequenceEqual(Token.Null))
                    {
                        AddNull();
                    }
                    else
                    {
                        Add(text);
                    }
                }
                else if (token.type == Token.Type.StartObject)
                {
                    JSONObject newObject = reader.ReadObject<JSONObject>();
                    Add(newObject);
                }
                else if (token.type == Token.Type.StartArray)
                {
                    JSONArray newArray = reader.ReadObject<JSONArray>();
                    Add(newArray);
                }
                else if (token.type == Token.Type.EndArray)
                {
                    break;
                }
            }
        }

        public static JSONArray Create()
        {
            Implementation* jsonArray = MemoryAddress.AllocatePointer<Implementation>();
            jsonArray->elements = new(4);
            return new(jsonArray);
        }

        private struct Implementation
        {
            public List<JSONProperty> elements;
        }
    }
}