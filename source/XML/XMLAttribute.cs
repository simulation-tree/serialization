using System;
using Unmanaged.Collections;

namespace Unmanaged.XML
{
    public readonly struct XMLAttribute : IDisposable
    {
        private readonly UnmanagedArray<char> name;
        private readonly UnmanagedArray<char> value;

        public readonly USpan<char> Name
        {
            get => name.AsSpan();
            set
            {
                uint newLength = value.length;
                if (newLength > name.Length)
                {
                    name.Resize(newLength);
                }

                value.CopyTo(name.AsSpan());
            }
        }

        public readonly USpan<char> Value
        {
            get => value.AsSpan();
            set
            {
                uint newLength = value.length;
                if (newLength > this.value.Length)
                {
                    this.value.Resize(newLength);
                }

                value.CopyTo(this.value.AsSpan());
            }
        }

        public readonly bool IsDisposed => name.IsDisposed;

        public XMLAttribute(USpan<char> name, USpan<char> value)
        {
            this.name = new(name);
            this.value = new(value);
        }

        public XMLAttribute(string name, string value)
        {
            this.name = new(name.AsUSpan());
            this.value = new(value.AsUSpan());
        }

        public XMLAttribute(ref XMLReader reader)
        {
            Token nameToken = reader.ReadToken();
            if (nameToken.type != Token.Type.Text)
            {
                throw new Exception();
            }

            USpan<char> buffer = stackalloc char[256];
            uint length = reader.GetText(nameToken, buffer);
            name = new(buffer.Slice(0, length));

            Token valueToken = reader.ReadToken();
            if (valueToken.type != Token.Type.Text)
            {
                throw new Exception();
            }

            length = reader.GetText(valueToken, buffer);
            value = new(buffer.Slice(0, length));
        }

        public readonly void Dispose()
        {
            name.Dispose();
            value.Dispose();
        }

        public unsafe readonly override string ToString()
        {
            using UnmanagedList<char> tempList = new(Name.length + Value.length + 3);
            uint length = ToString(tempList);
            return new string(tempList.AsSpan().pointer, 0, (int)length);
        }

        public readonly uint ToString(UnmanagedList<char> list)
        {
            uint count = list.Count;
            list.AddRange(Name);
            list.Add('=');
            list.Add('"');
            list.AddRange(Value);
            list.Add('"');
            return list.Count - count;
        }
    }
}