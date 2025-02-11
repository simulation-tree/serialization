using Collections;
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
        private Implementation* value;

        public readonly USpan<JSONProperty> Properties => value->properties.AsSpan();
        public readonly uint Count => value->properties.Count;
        public readonly bool IsDisposed => value is null;

        public readonly ref JSONProperty this[uint index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref value->properties[index];
            }
        }

        public readonly ref JSONProperty this[USpan<char> name]
        {
            get
            {
                uint count = Count;
                for (uint i = 0; i < count; i++)
                {
                    ref JSONProperty property = ref Properties[i];
                    if (property.Name.SequenceEqual(name))
                    {
                        return ref property;
                    }
                }

                throw new NullReferenceException($"Property `{name.ToString()}` not found");
            }
        }

        public readonly ref JSONProperty this[string name] => ref this[name.AsSpan()];

        public readonly nint Address => (nint)value;

#if NET
        /// <summary>
        /// Creates a new empty JSON object.
        /// </summary>
        public JSONObject()
        {
            value = Implementation.Allocate();
        }
#endif

        public JSONObject(void* value)
        {
            this.value = (Implementation*)value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            Implementation.Free(ref value);
        }

        public readonly void Clear()
        {
            value->properties.Clear();
        }

        public readonly void RemoveAt(uint index)
        {
            ThrowIfDisposed();

            value->properties.RemoveAtBySwapping(index);
        }

        public readonly T As<T>() where T : unmanaged, IJSONSerializable
        {
            ThrowIfDisposed();
            T value = default;
            using Text result = new(0);
            ToString(result);
            using BinaryReader reader = BinaryReader.CreateFromUTF8(result.AsSpan());
            JSONReader jsonReader = new(reader);
            jsonReader.ReadToken(out _);
            value.Read(jsonReader);
            return value;
        }

        public readonly void ToString(Text result, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            ThrowIfDisposed();

            result.Append('{');
            if (value->properties.Count > 0)
            {
                NewLine();
                for (int i = 0; i <= depth; i++)
                {
                    Indent(indent);
                }

                uint position = 0;
                while (true)
                {
                    ref JSONProperty property = ref value->properties[position];
                    byte childDepth = depth;
                    childDepth++;
                    property.ToString(result, true, indent, cr, lf, childDepth);
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

            result.Append('}');

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

        public readonly void Add(USpan<char> name, USpan<char> text)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, text);
            value->properties.Add(property);
        }

        public readonly void Add(string name, string text)
        {
            Add(name.AsSpan(), text.AsSpan());
        }

        public readonly void Add(USpan<char> name, double number)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, number);
            value->properties.Add(property);
        }

        public readonly void Add(string name, double number)
        {
            Add(name.AsSpan(), number);
        }

        public readonly void Add(USpan<char> name, bool boolean)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, boolean);
            value->properties.Add(property);
        }

        public readonly void Add(string name, bool boolean)
        {
            Add(name.AsSpan(), boolean);
        }

        public readonly void Add(USpan<char> name, JSONObject obj)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, obj);
            value->properties.Add(property);
        }

        public readonly void Add(string name, JSONObject obj)
        {
            Add(name.AsSpan(), obj);
        }

        public readonly void Add(USpan<char> name, JSONArray array)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, array);
            value->properties.Add(property);
        }

        public readonly void Add(string name, JSONArray array)
        {
            Add(name.AsSpan(), array);
        }

        public readonly void AddNull(USpan<char> name)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name);
            value->properties.Add(property);
        }

        public readonly void AddNull(string name)
        {
            AddNull(name.AsSpan());
        }

        public readonly bool Contains(USpan<char> name)
        {
            ThrowIfDisposed();

            uint count = Count;
            for (uint i = 0; i < count; i++)
            {
                ref JSONProperty property = ref value->properties[i];
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

        public readonly void Set(USpan<char> name, USpan<char> text)
        {
            ThrowIfDisposed();

            JSONProperty property = this[name];
            property.Text = text;
        }

        public readonly void Set(string name, string text)
        {
            Set(name.AsSpan(), text.AsSpan());
        }

        public readonly USpan<char> GetText(USpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Text;
        }

        public readonly USpan<char> GetText(string name)
        {
            return GetText(name.AsSpan());
        }

        public readonly ref double GetNumber(USpan<char> name)
        {
            ThrowIfDisposed();

            return ref this[name].Number;
        }

        public readonly ref double GetNumber(string name)
        {
            return ref GetNumber(name.AsSpan());
        }

        public readonly ref bool GetBoolean(USpan<char> name)
        {
            ThrowIfDisposed();

            return ref this[name].Boolean;
        }

        public readonly ref bool GetBoolean(string name)
        {
            return ref GetBoolean(name.AsSpan());
        }

        public readonly JSONObject GetObject(USpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Object;
        }

        public readonly JSONObject GetObject(string name)
        {
            return GetObject(name.AsSpan());
        }

        public readonly JSONArray GetArray(USpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Array;
        }

        public readonly JSONArray GetArray(string name)
        {
            return GetArray(name.AsSpan());
        }

        public readonly bool TryGetText(USpan<char> name, out USpan<char> text)
        {
            ThrowIfDisposed();

            if (!Contains(name))
            {
                text = default;
                return false;
            }

            return this[name].TryGetText(out text);
        }

        public readonly bool TryGetText(string name, out USpan<char> text)
        {
            return TryGetText(name.AsSpan(), out text);
        }

        public readonly bool TryGetNumber(USpan<char> name, out double number)
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

        public readonly bool TryGetBoolean(USpan<char> name, out bool boolean)
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

        public readonly bool TryGetObject(USpan<char> name, out JSONObject obj)
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

        public readonly bool TryGetArray(USpan<char> name, out JSONArray array)
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

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            Text list = new(0);
            ToString(list);
            writer.WriteUTF8(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = Implementation.Allocate();
            JSONReader jsonReader = new(reader);
            if (jsonReader.PeekToken(out Token nextToken))
            {
                if (nextToken.type == Token.Type.StartObject)
                {
                    jsonReader.ReadToken(out _);
                }
            }

            ParseObject(jsonReader, reader, this);

            static void ParseObject(JSONReader jsonReader, BinaryReader reader, JSONObject jsonObject)
            {
                USpan<char> buffer = stackalloc char[256];
                while (jsonReader.ReadToken(out Token token))
                {
                    if (token.type == Token.Type.Text)
                    {
                        uint length = jsonReader.GetText(token, buffer);
                        if (jsonReader.ReadToken(out Token nextToken))
                        {
                            USpan<char> nameSpan = buffer.Slice(0, length);
                            if (nameSpan.Length > 0 && nameSpan[0] == '"')
                            {
                                nameSpan = nameSpan.Slice(1, nameSpan.Length - 2);
                            }

                            if (nextToken.type == Token.Type.True)
                            {
                                jsonObject.Add(nameSpan, true);
                            }
                            else if (nextToken.type == Token.Type.False)
                            {
                                jsonObject.Add(nameSpan, false);
                            }
                            else if (nextToken.type == Token.Type.Null)
                            {
                                jsonObject.AddNull(nameSpan);
                            }
                            else if (nextToken.type == Token.Type.Number)
                            {
                                jsonObject.Add(nameSpan, jsonReader.GetNumber(nextToken));
                            }
                            else if (nextToken.type == Token.Type.Text)
                            {
                                Text textBuffer = new(nextToken.length * 4);
                                USpan<char> bufferSpan = textBuffer.AsSpan();
                                uint textLength = jsonReader.GetText(nextToken, bufferSpan);
                                USpan<char> text = bufferSpan.Slice(0, textLength);
                                if (text.Length > 0 && text[0] == '"')
                                {
                                    text = text.Slice(1, text.Length - 2);
                                }

                                jsonObject.Add(nameSpan, text);
                                textBuffer.Dispose();
                            }
                            else if (nextToken.type == Token.Type.StartObject)
                            {
                                JSONObject newObject = reader.ReadObject<JSONObject>();
                                jsonObject.Add(nameSpan, newObject);
                            }
                            else if (nextToken.type == Token.Type.StartArray)
                            {
                                JSONArray newArray = reader.ReadObject<JSONArray>();
                                jsonObject.Add(nameSpan, newArray);
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
                            throw new InvalidOperationException($"Invalid JSON token at position {token.position}, expected value.");
                        }
                    }
                    else if (token.type == Token.Type.EndObject)
                    {
                        break;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid JSON token at position {token.position}");
                    }
                }
            }
        }

        public static JSONObject Create()
        {
            return new(Implementation.Allocate());
        }

        public readonly struct Implementation
        {
            public readonly List<JSONProperty> properties;

            private Implementation(List<JSONProperty> properties)
            {
                this.properties = properties;
            }

            public static Implementation* Allocate()
            {
                List<JSONProperty> properties = new(4);
                ref Implementation value = ref Allocations.Allocate<Implementation>();
                value = new(properties);
                fixed (Implementation* pointer = &value)
                {
                    return pointer;
                }
            }

            public static void Free(ref Implementation* obj)
            {
                Allocations.ThrowIfNull(obj);

                uint count = obj->properties.Count;
                for (uint i = 0; i < count; i++)
                {
                    JSONProperty property = obj->properties[i];
                    property.Dispose();
                }

                obj->properties.Dispose();
                Allocations.Free(ref obj);
            }
        }
    }
}