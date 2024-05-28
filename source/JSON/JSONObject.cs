using System;
using System.Diagnostics;
using Unmanaged.Collections;
using Unmanaged.JSON.Unsafe;

namespace Unmanaged.JSON
{
    public unsafe struct JSONObject : IDisposable
    {
        private UnsafeJSONObject* value;

        public readonly UnmanagedList<JSONProperty> Properties => UnsafeJSONObject.GetProperties(value);
        public readonly uint Count => Properties.Count;
        public readonly bool IsDisposed => UnsafeJSONObject.IsDisposed(value);

        public readonly JSONProperty this[uint index]
        {
            get
            {
                ThrowIfDisposed();
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                UnmanagedList<JSONProperty> properties = Properties;
                ref JSONProperty property = ref properties.GetRef(index);
                return property;
            }
        }

        public readonly JSONProperty this[ReadOnlySpan<char> name]
        {
            get
            {
                ThrowIfDisposed();

                UnmanagedList<JSONProperty> properties = Properties;
                uint count = Count;
                for (uint i = 0; i < count; i++)
                {
                    ref JSONProperty property = ref properties.GetRef(i);
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

        public JSONObject(ReadOnlySpan<byte> jsonBytes)
        {
            value = UnsafeJSONObject.Allocate();
            JSONReader reader = new(jsonBytes);
            ParseFrom(ref reader);
            reader.Dispose();
        }

        public JSONObject(ReadOnlySpan<char> jsonText)
        {
            value = UnsafeJSONObject.Allocate();
            JSONReader reader = new(jsonText);
            ParseFrom(ref reader);
            reader.Dispose();
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeJSONObject.Free(ref value);
        }

        public readonly void Clear()
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = Properties;
            properties.Clear();
        }

        public readonly void RemoveAt(uint index)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = Properties;
            properties.RemoveAtBySwapping(index);
        }

        public readonly T As<T>() where T : unmanaged, IJSONObject
        {
            ThrowIfDisposed();
            T value = default;
            using UnmanagedList<char> result = new();
            ToString(result);
            JSONReader reader = new(result.AsSpan());
            reader.ReadToken(out _);
            value.Read(ref reader);
            reader.Dispose();
            return value;
        }

        public readonly void ParseFrom(ref JSONReader reader)
        {
            ThrowIfDisposed();
            if (reader.PeekToken(out Token nextToken))
            {
                if (nextToken.type == Token.Type.StartObject)
                {
                    reader.ReadToken(out _);
                }
            }

            ParseObject(ref reader, this);

            static void ParseObject(ref JSONReader reader, JSONObject jsonObject)
            {
                Span<char> name = stackalloc char[256];
                while (reader.ReadToken(out Token token))
                {
                    if (token.type == Token.Type.Text)
                    {
                        name.Clear();
                        ReadOnlySpan<char> source = reader.GetText(token);
                        source.CopyTo(name);
                        if (reader.ReadToken(out Token nextToken))
                        {
                            if (nextToken.type == Token.Type.True)
                            {
                                jsonObject.Add(name[..source.Length], true);
                            }
                            else if (nextToken.type == Token.Type.False)
                            {
                                jsonObject.Add(name[..source.Length], false);
                            }
                            else if (nextToken.type == Token.Type.Null)
                            {
                                jsonObject.AddNull(name[..source.Length]);
                            }
                            else if (nextToken.type == Token.Type.Number)
                            {
                                jsonObject.Add(name[..source.Length], reader.GetNumber(nextToken));
                            }
                            else if (nextToken.type == Token.Type.Text)
                            {
                                jsonObject.Add(name[..source.Length], reader.GetText(nextToken));
                            }
                            else if (nextToken.type == Token.Type.StartObject)
                            {
                                JSONObject obj = new();
                                obj.ParseFrom(ref reader);
                                jsonObject.Add(name[..source.Length], obj);
                            }
                            else if (nextToken.type == Token.Type.StartArray)
                            {
                                JSONArray array = new();
                                array.ParseFrom(ref reader);
                                jsonObject.Add(name[..source.Length], array);
                            }
                            else if (nextToken.type == Token.Type.EndObject)
                            {
                                break;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Invalid JSON token at position {nextToken.position}, \"{reader.GetText(nextToken).ToString()}\".");
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
                        throw new InvalidOperationException($"Invalid JSON token at position {token.position}, \"{reader.GetText(token).ToString()}\".");
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
            UnmanagedList<JSONProperty> properties = Properties;
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

            UnmanagedList<JSONProperty> properties = Properties;
            JSONProperty property = new(name, text);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, double number)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = Properties;
            JSONProperty property = new(name, number);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, bool boolean)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = Properties;
            JSONProperty property = new(name, boolean);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, JSONObject obj)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = Properties;
            JSONProperty property = new(name, obj);
            properties.Add(property);
        }

        public readonly void Add(ReadOnlySpan<char> name, JSONArray array)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = Properties;
            JSONProperty property = new(name, array);
            properties.Add(property);
        }

        public readonly void AddNull(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            UnmanagedList<JSONProperty> properties = Properties;
            JSONProperty property = new(name);
            properties.Add(property);
        }

        public readonly bool Contains(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();
            UnmanagedList<JSONProperty> properties = Properties;
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
    }
}