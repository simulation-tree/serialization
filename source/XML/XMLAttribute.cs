using System;
using Unmanaged.Collections;

namespace Unmanaged.XML
{
    public struct XMLAttribute : IDisposable
    {
        private UnmanagedArray<char> name;
        private UnmanagedArray<char> value;

        public readonly ReadOnlySpan<char> Name
        {
            get => name.AsSpan();
            set
            {
                uint newLength = (uint)value.Length;
                if (newLength > name.Length)
                {
                    name.Resize(newLength);
                }

                value.CopyTo(name.AsSpan());
            }
        }

        public readonly ReadOnlySpan<char> Value
        {
            get => value.AsSpan();
            set
            {
                uint newLength = (uint)value.Length;
                if (newLength > this.value.Length)
                {
                    this.value.Resize(newLength);
                }

                value.CopyTo(this.value.AsSpan());
            }
        }

        public readonly bool IsDisposed => name.IsDisposed;

        public XMLAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            this.name = new(name);
            this.value = new(value);
        }

        public XMLAttribute(ref XMLReader reader)
        {
            Token nameToken = reader.ReadToken();
            if (nameToken.type != Token.Type.Text)
            {
                throw new Exception();
            }

            Span<char> buffer = stackalloc char[256];
            int length = reader.GetText(nameToken, buffer);
            name = new(buffer[..length]);

            Token valueToken = reader.ReadToken();
            if (valueToken.type != Token.Type.Text)
            {
                throw new Exception();
            }

            length = reader.GetText(valueToken, buffer);
            value = new(buffer[..length]);
        }

        public void Dispose()
        {
            name.Dispose();
            value.Dispose();
        }

        public readonly void ToString(UnmanagedList<char> list)
        {
            list.AddRange(Name);
            list.Add('=');
            list.Add('"');
            list.AddRange(Value);
            list.Add('"');
        }

        public readonly override string ToString()
        {
            UnmanagedList<char> list = new();
            ToString(list);
            string text = list.AsSpan().ToString();
            list.Dispose();
            return text;
        }
    }
}