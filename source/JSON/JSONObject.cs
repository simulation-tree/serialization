using System;
using System.Diagnostics;
using Unmanaged.Collections;
using Unmanaged.JSON.Unsafe;

namespace Unmanaged.JSON
{
    public unsafe struct JSONObject : IDisposable, ISerializable
    {
        private UnsafeJSONObject* value;

        private readonly UnmanagedList<JSONProperty> PropertiesList => UnsafeJSONObject.GetProperties(value);

        public readonly ReadOnlySpan<JSONProperty> Properties => PropertiesList.AsSpan();
        public readonly uint Count => PropertiesList.Count;
        public readonly bool IsDisposed => UnsafeJSONObject.IsDisposed(value);

        public readonly JSONProperty this[uint index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                UnmanagedList<JSONProperty> properties = PropertiesList;
                ref JSONProperty property = ref properties.GetRef(index);
                return property;
            }
        }

        public readonly JSONProperty this[ReadOnlySpan<char> name]
        {
            get
            {
                uint count = Count;
                for (int i = 0; i < count; i++)
                {
                    JSONProperty property = Properties[i];
                    if (property.Name.Equals(name, StringComparison.Ordinal))
                    {
                        return property;
                    }
                }

                throw new NullReferenceException($"Property \"{name.ToString()}\" not found.");
            }
        }

        public readonly nint Address => (nint)value;

        public JSONObject()
        {
            value = UnsafeJSONObject.Allocate();
        }

        public JSONObject(void* value)
        {
            this.value = (UnsafeJSONObject*)value;
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
            using UnmanagedList<char> result = new();
            ToString(result);
            using BinaryReader reader = BinaryReader.CreateFromUTF8(result.AsSpan());
            JSONReader jsonReader = new(reader);
            jsonReader.ReadToken(out _);
            value.Read(jsonReader);
            return value;
        }

        public readonly void ToString(UnmanagedList<char> result, ReadOnlySpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
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
                    ref JSONProperty property = ref properties.GetRef(position);
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

            void Indent(ReadOnlySpan<char> indent) 
            {
                result.AddRange(indent);
            }
        }

        public readonly override string ToString()
        {
            UnmanagedList<char> buffer = new();
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

        public readonly void Add(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, text);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, double number)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, number);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, bool boolean)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, boolean);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, JSONObject obj)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, obj);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, JSONArray array)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name, array);
            properties.Add(property);
        }

        public readonly void AddNull(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = PropertiesList;
            JSONProperty property = new(name);
            properties.Add(property);
        }

        public readonly bool Contains(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = PropertiesList;
            uint count = Count;
            for (uint i = 0; i < count; i++)
            {
                ref JSONProperty property = ref properties.GetRef(i);
                if (property.Name.Equals(name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly void Set(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();
            JSONProperty property = this[name];
            property.Text = text;
        }

        public readonly ReadOnlySpan<char> GetText(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            return this[name].Text;
        }

        public readonly ref double GetNumber(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            return ref this[name].Number;
        }

        public readonly ref bool GetBoolean(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            return ref this[name].Boolean;
        }

        public readonly JSONObject GetObject(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            return this[name].Object;
        }

        public readonly JSONArray GetArray(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            return this[name].Array;
        }

        public readonly bool TryGetText(ReadOnlySpan<char> name, out ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();
            return this[name].TryGetText(out text);
        }

        public readonly bool TryGetNumber(ReadOnlySpan<char> name, out double number)
        {
            ThrowIfDisposed();
            return this[name].TryGetNumber(out number);
        }

        public readonly bool TryGetBoolean(ReadOnlySpan<char> name, out bool boolean)
        {
            ThrowIfDisposed();
            return this[name].TryGetBoolean(out boolean);
        }

        public readonly bool TryGetObject(ReadOnlySpan<char> name, out JSONObject obj)
        {
            ThrowIfDisposed();
            return this[name].TryGetObject(out obj);
        }

        public readonly bool TryGetArray(ReadOnlySpan<char> name, out JSONArray array)
        {
            ThrowIfDisposed();
            return this[name].TryGetArray(out array);
        }

        void ISerializable.Write(BinaryWriter writer)
        {
            UnmanagedList<char> list = new();
            ToString(list);
            writer.WriteUTF8Span(list.AsSpan());
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
                Span<char> buffer = stackalloc char[256];
                while (jsonReader.ReadToken(out Token token))
                {
                    if (token.type == Token.Type.Text)
                    {
                        int length = jsonReader.GetText(token, buffer);
                        if (jsonReader.ReadToken(out Token nextToken))
                        {
                            Span<char> nameSpan = buffer[..length].TrimStart('"').TrimEnd('"');
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
                                Span<char> textSpan = listBuffer.AsSpan();
                                int textLength = jsonReader.GetText(nextToken, textSpan);
                                jsonObject.Add(nameSpan, textSpan[..textLength].TrimStart('"').TrimEnd('"'));
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
    }
}