using System;
using System.Diagnostics;
using Unmanaged.Collections;
using Unmanaged.JSON.Unsafe;

namespace Unmanaged.JSON
{
    /// <summary>
    /// Abstract object able to contain any JSON structure.
    /// </summary>
    public unsafe struct JSONObject : IDisposable, ISerializable
    {
        private UnsafeJSONObject* value;

        private readonly UnmanagedList<JSONProperty> PropertiesList => UnsafeJSONObject.GetProperties(value);

        public readonly USpan<JSONProperty> Properties => PropertiesList.AsSpan();
        public readonly uint Count => PropertiesList.Count;
        public readonly bool IsDisposed => UnsafeJSONObject.IsDisposed(value);

        public readonly ref JSONProperty this[uint index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                UnmanagedList<JSONProperty> properties = PropertiesList;
                return ref properties[index];
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

                throw new NullReferenceException($"Property \"{name.ToString()}\" not found.");
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
            value = UnsafeJSONObject.Allocate();
        }
#endif

        internal JSONObject(void* value)
        {
            this.value = (UnsafeJSONObject*)value;
        }

        private JSONObject(UnsafeJSONObject* value)
        {
            this.value = value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeJSONObject.Free(ref value);
        }

        public readonly void Clear()
        {
            UnmanagedList<JSONProperty> properties = PropertiesList;
            properties.Clear();
        }

        public readonly void RemoveAt(uint index)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            properties.RemoveAtBySwapping(index);
        }

        public readonly T As<T>() where T : unmanaged, IJSONSerializable
        {
            ThrowIfDisposed();
            T value = default;
            using UnmanagedList<char> result = UnmanagedList<char>.Create();
            ToString(result);
            using BinaryReader reader = BinaryReader.CreateFromUTF8(result.AsSpan());
            JSONReader jsonReader = new(reader);
            jsonReader.ReadToken(out _);
            value.Read(jsonReader);
            return value;
        }

        public readonly void ToString(UnmanagedList<char> result, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            result.Add('{');
            if (properties.Count > 0)
            {
                NewLine();
                for (byte i = 0; i <= depth; i++)
                {
                    Indent(indent);
                }

                uint position = 0;
                while (true)
                {
                    ref JSONProperty property = ref properties[position];
                    byte childDepth = depth;
                    childDepth++;
                    property.ToString(result, true, indent, cr, lf, childDepth);
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

            result.Add('}');

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
            UnmanagedList<char> buffer = UnmanagedList<char>.Create();
            ToString(buffer);
            string result = buffer.AsSpan().ToString();
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
            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, text);
            properties.Add(property);
        }

        public readonly void Add(string name, string text)
        {
            Add(name.AsSpan(), text.AsSpan());
        }

        public readonly void Add(USpan<char> name, double number)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, number);
            properties.Add(property);
        }

        public readonly void Add(string name, double number)
        {
            Add(name.AsSpan(), number);
        }

        public readonly void Add(USpan<char> name, bool boolean)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, boolean);
            properties.Add(property);
        }

        public readonly void Add(string name, bool boolean)
        {
            Add(name.AsSpan(), boolean);
        }

        public readonly void Add(USpan<char> name, JSONObject obj)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, obj);
            properties.Add(property);
        }

        public readonly void Add(string name, JSONObject obj)
        {
            Add(name.AsSpan(), obj);
        }

        public readonly void Add(USpan<char> name, JSONArray array)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, array);
            properties.Add(property);
        }

        public readonly void Add(string name, JSONArray array)
        {
            Add(name.AsSpan(), array);
        }

        public readonly void AddNull(USpan<char> name)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name);
            properties.Add(property);
        }

        public readonly void AddNull(string name)
        {
            AddNull(name.AsSpan());
        }

        public readonly bool Contains(USpan<char> name)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            uint count = Count;
            for (uint i = 0; i < count; i++)
            {
                ref JSONProperty property = ref properties[i];
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
            UnmanagedList<char> list = UnmanagedList<char>.Create();
            ToString(list);
            writer.WriteUTF8Text(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = UnsafeJSONObject.Allocate();
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
                            if (nameSpan.length > 0 && nameSpan[0] == '"')
                            {
                                nameSpan = nameSpan.Slice(1, nameSpan.length - 2);
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
                                UnmanagedArray<char> listBuffer = new(nextToken.length * 4);
                                USpan<char> textSpan = listBuffer.AsSpan();
                                uint textLength = jsonReader.GetText(nextToken, textSpan);
                                USpan<char> text = textSpan.Slice(0, textLength);
                                if (text.length > 0 && text[0] == '"')
                                {
                                    text = text.Slice(1, text.length - 2);
                                }

                                jsonObject.Add(nameSpan, text);
                                listBuffer.Dispose();
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
            return new(UnsafeJSONObject.Allocate());
        }
    }
}