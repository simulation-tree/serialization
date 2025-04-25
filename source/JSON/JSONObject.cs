using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.JSON
{
    /// <summary>
    /// Abstract object able to contain any JSON structure.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct JSONObject : IDisposable, ISerializable
    {
        private Implementation* jsonObject;

        public readonly ReadOnlySpan<JSONProperty> Properties
        {
            get
            {
                ThrowIfDisposed();

                return jsonObject->properties.AsSpan();
            }
        }

        public readonly int Count
        {
            get
            {
                ThrowIfDisposed();

                return jsonObject->properties.Count;
            }
        }

        public readonly bool IsDisposed => jsonObject is null;

        public readonly ref JSONProperty this[int index]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfPropertyIndexIsOutOfRange(index);

                return ref jsonObject->properties[index];
            }
        }

        public readonly ref JSONProperty this[ReadOnlySpan<char> name]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfPropertyIsMissing(name);

                int count = Count;
                for (int i = 0; i < count; i++)
                {
                    ref JSONProperty property = ref jsonObject->properties[i];
                    if (property.Name.SequenceEqual(name))
                    {
                        return ref property;
                    }
                }

                return ref Unsafe.AsRef<JSONProperty>(default);
            }
        }

        public readonly ref JSONProperty this[string name]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfPropertyIsMissing(name);

                int count = Count;
                for (int i = 0; i < count; i++)
                {
                    ref JSONProperty property = ref jsonObject->properties[i];
                    if (property.Name.SequenceEqual(name))
                    {
                        return ref property;
                    }
                }

                return ref Unsafe.AsRef<JSONProperty>(default);
            }
        }

        public readonly nint Address => (nint)jsonObject;

#if NET
        /// <summary>
        /// Creates a new empty JSON object.
        /// </summary>
        public JSONObject()
        {
            jsonObject = MemoryAddress.AllocatePointer<Implementation>();
            jsonObject->properties = new(4);
        }
#endif

        public JSONObject(void* value)
        {
            this.jsonObject = (Implementation*)value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            Span<JSONProperty> properties = jsonObject->properties.AsSpan();
            for (int i = 0; i < properties.Length; i++)
            {
                properties[i].Dispose();
            }

            jsonObject->properties.Dispose();
            MemoryAddress.Free(ref jsonObject);
        }

        public readonly void Clear()
        {
            ThrowIfDisposed();

            jsonObject->properties.Clear();
        }

        public readonly void RemoveAt(int index)
        {
            ThrowIfDisposed();

            jsonObject->properties.RemoveAtBySwapping(index);
        }

        public readonly T As<T>() where T : unmanaged, IJSONSerializable
        {
            ThrowIfDisposed();

            T value = default;
            using Text result = new(0);
            ToString(result);
            using ByteReader reader = ByteReader.CreateFromUTF8(result.AsSpan());
            JSONReader jsonReader = new(reader);
            jsonReader.ReadToken(out _);
            value.Read(jsonReader);
            return value;
        }

        public readonly void ToString(Text result, SerializationSettings settings = default)
        {
            ToString(result, settings, 0);
        }

        internal readonly void ToString(Text result, SerializationSettings settings, byte depth)
        {
            ThrowIfDisposed();

            result.Append('{');
            if (jsonObject->properties.Count > 0)
            {
                settings.NewLine(result);
                for (int i = 0; i <= depth; i++)
                {
                    settings.Indent(result);
                }

                int position = 0;
                while (true)
                {
                    ref JSONProperty property = ref jsonObject->properties[position];
                    byte childDepth = depth;
                    childDepth++;
                    result.Append('\"');
                    result.Append(property.Name);
                    result.Append('\"');
                    result.Append(':');
                    property.ToString(result, settings, childDepth);
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

            result.Append('}');
        }

        public readonly override string ToString()
        {
            ThrowIfDisposed();

            Text buffer = new(0);
            ToString(buffer);
            string result = buffer.ToString();
            buffer.Dispose();
            return result;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(JSONObject));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPropertyIsMissing(ReadOnlySpan<char> name)
        {
            if (!Contains(name))
            {
                throw new NullReferenceException($"Property `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPropertyIndexIsOutOfRange(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException($"Property index `{index}` is out of range");
            }
        }

        public readonly void Add(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, text);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(string name, string text)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, text);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, double number)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, number);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(string name, double number)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, number);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, bool boolean)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, boolean);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(string name, bool boolean)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, boolean);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, JSONObject jsonObject)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonObject);
            this.jsonObject->properties.Add(property);
        }

        public readonly void Add(string name, JSONObject jsonObject)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonObject);
            this.jsonObject->properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, JSONArray jsonArray)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonArray);
            jsonObject->properties.Add(property);
        }

        public readonly void Add(string name, JSONArray jsonArray)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonArray);
            jsonObject->properties.Add(property);
        }

        public readonly void AddNull(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name);
            jsonObject->properties.Add(property);
        }

        public readonly void AddNull(string name)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name);
            jsonObject->properties.Add(property);
        }

        public readonly bool Contains(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            int count = Count;
            for (int i = 0; i < count; i++)
            {
                ref JSONProperty property = ref jsonObject->properties[i];
                if (property.Name.SequenceEqual(name))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Contains(string name)
        {
            return Contains(name.AsSpan());
        }

        public readonly void Set(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            JSONProperty property = this[name];
            property.Text = text;
        }

        public readonly void Set(string name, string text)
        {
            Set(name.AsSpan(), text.AsSpan());
        }

        public readonly ReadOnlySpan<char> GetText(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Text;
        }

        public readonly ReadOnlySpan<char> GetText(string name)
        {
            return GetText(name.AsSpan());
        }

        public readonly ref double GetNumber(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return ref this[name].Number;
        }

        public readonly ref double GetNumber(string name)
        {
            return ref GetNumber(name.AsSpan());
        }

        public readonly ref bool GetBoolean(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return ref this[name].Boolean;
        }

        public readonly ref bool GetBoolean(string name)
        {
            return ref GetBoolean(name.AsSpan());
        }

        public readonly JSONObject GetObject(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Object;
        }

        public readonly JSONObject GetObject(string name)
        {
            return GetObject(name.AsSpan());
        }

        public readonly JSONArray GetArray(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Array;
        }

        public readonly JSONArray GetArray(string name)
        {
            return GetArray(name.AsSpan());
        }

        public readonly bool TryGetText(ReadOnlySpan<char> name, out ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            if (!Contains(name))
            {
                text = default;
                return false;
            }

            return this[name].TryGetText(out text);
        }

        public readonly bool TryGetText(string name, out ReadOnlySpan<char> text)
        {
            return TryGetText(name.AsSpan(), out text);
        }

        public readonly bool TryGetNumber(ReadOnlySpan<char> name, out double number)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                number = default;
                return false;
            }

            return this[name].TryGetNumber(out number);
        }

        public readonly bool TryGetNumber(string name, out double number)
        {
            return TryGetNumber(name.AsSpan(), out number);
        }

        public readonly bool TryGetBoolean(ReadOnlySpan<char> name, out bool boolean)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                boolean = default;
                return false;
            }

            return this[name].TryGetBoolean(out boolean);
        }

        public readonly bool TryGetBoolean(string name, out bool boolean)
        {
            return TryGetBoolean(name.AsSpan(), out boolean);
        }

        public readonly bool TryGetObject(ReadOnlySpan<char> name, out JSONObject obj)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                obj = default;
                return false;
            }

            return this[name].TryGetObject(out obj);
        }

        public readonly bool TryGetObject(string name, out JSONObject obj)
        {
            return TryGetObject(name.AsSpan(), out obj);
        }

        public readonly bool TryGetArray(ReadOnlySpan<char> name, out JSONArray array)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                array = default;
                return false;
            }

            return this[name].TryGetArray(out array);
        }

        public readonly bool TryGetArray(string name, out JSONArray array)
        {
            return TryGetArray(name.AsSpan(), out array);
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
            jsonObject = MemoryAddress.AllocatePointer<Implementation>();
            jsonObject->properties = new(4);
            JSONReader jsonReader = new(reader);
            if (jsonReader.PeekToken(out Token nextToken, out int readBytes))
            {
                if (nextToken.type == Token.Type.StartObject)
                {
                    //start of object
                    reader.Advance(readBytes);
                }
            }

            //todo: share these temp buffers?
            using Text nameTextBuffer = new(256);
            using Text nextTextBuffer = new(256);
            while (jsonReader.ReadToken(out Token token))
            {
                if (token.type == Token.Type.Text)
                {
                    int capacity = token.length * 4;
                    if (nameTextBuffer.Length < capacity)
                    {
                        nameTextBuffer.SetLength(capacity);
                    }

                    int nameTextLength = jsonReader.GetText(token, nameTextBuffer.AsSpan());
                    Span<char> name = nameTextBuffer.Slice(0, nameTextLength);
                    if (jsonReader.ReadToken(out nextToken))
                    {
                        int nextCapacity = nextToken.length * 4;
                        if (nextTextBuffer.Length < nextCapacity)
                        {
                            nextTextBuffer.SetLength(nextCapacity);
                        }

                        if (nextToken.type == Token.Type.Text)
                        {
                            int nextTextLength = jsonReader.GetText(nextToken, nextTextBuffer.AsSpan());
                            ReadOnlySpan<char> nextText = nextTextBuffer.Slice(0, nextTextLength);
                            if (double.TryParse(nextText, out double number))
                            {
                                Add(name, number);
                            }
                            else if (nextText.SequenceEqual(Token.True))
                            {
                                Add(name, true);
                            }
                            else if (nextText.SequenceEqual(Token.False))
                            {
                                Add(name, false);
                            }
                            else if (nextText.SequenceEqual(Token.Null))
                            {
                                AddNull(name);
                            }
                            else
                            {
                                Add(name, nextText);
                            }
                        }
                        else if (nextToken.type == Token.Type.StartObject)
                        {
                            JSONObject newObject = reader.ReadObject<JSONObject>();
                            Add(name, newObject);
                        }
                        else if (nextToken.type == Token.Type.StartArray)
                        {
                            JSONArray newArray = reader.ReadObject<JSONArray>();
                            Add(name, newArray);
                        }
                        else if (nextToken.type == Token.Type.EndObject)
                        {
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid JSON token at position {nextToken.position}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"No succeeding token available after {name.ToString()}");
                    }
                }
                else if (token.type == Token.Type.EndObject)
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected token `{token.type}`, expected }} or another text token");
                }
            }
        }

        public static JSONObject Create()
        {
            Implementation* jsonObject = MemoryAddress.AllocatePointer<Implementation>();
            jsonObject->properties = new(4);
            return new JSONObject(jsonObject);
        }

        private struct Implementation
        {
            public List<JSONProperty> properties;
        }
    }
}