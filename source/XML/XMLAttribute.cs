using System;
using Unmanaged;

namespace Serialization.XML
{
    public readonly struct XMLAttribute : IDisposable
    {
        private readonly Text name;
        private readonly Text value;

        public readonly USpan<char> Name
        {
            get => name.AsSpan();
            set => name.CopyFrom(value);
        }

        public readonly USpan<char> Value
        {
            get => value.AsSpan();
            set => value.CopyFrom(value);
        }

        public readonly bool IsDisposed => name.IsDisposed;

        public XMLAttribute(USpan<char> name, USpan<char> value)
        {
            this.name = new(name);
            this.value = new(value);
        }

        public XMLAttribute(string name, string value)
        {
            this.name = new(name);
            this.value = new(value);
        }

        public XMLAttribute(ref XMLReader reader)
        {
            Token nameToken = reader.ReadToken();
            if (nameToken.type != Token.Type.Text)
            {
                throw new();
            }

            USpan<char> buffer = stackalloc char[256];
            uint length = reader.GetText(nameToken, buffer);
            name = new(buffer.Slice(0, length));

            Token valueToken = reader.ReadToken();
            if (valueToken.type != Token.Type.Text)
            {
                throw new();
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
            Text tempList = new(0);
            ToString(tempList);
            string result = tempList.ToString();
            tempList.Dispose();
            return result;
        }

        public readonly void ToString(Text destination)
        {
            destination.Append(Name);
            destination.Append('=');
            destination.Append('"');
            destination.Append(Value);
            destination.Append('"');
        }
    }
}