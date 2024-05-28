using System;
using Unmanaged.Collections;

namespace Unmanaged.XML
{
    public struct XMLNode : IDisposable, IBinaryObject
    {
        private UnmanagedArray<char> name;
        private UnmanagedList<XMLAttribute> attributes;
        private UnmanagedList<char> content;
        private UnmanagedList<XMLNode> children;

        /// <summary>
        /// Name of the node.
        /// </summary>
        public readonly Span<char> Name
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

        /// <summary>
        /// Possible text content inside the node.
        /// </summary>
        public readonly ReadOnlySpan<char> Content
        {
            get => content.AsSpan();
            set
            {
                content.Clear();
                content.AddRange(value);
            }
        }

        public readonly ref XMLAttribute this[uint index]
        {
            get
            {
                if (index >= attributes.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref attributes.GetRef(index);
            }
        }

        /// <summary>
        /// Attributes defining the node.
        /// </summary>
        public readonly ReadOnlySpan<XMLAttribute> Attributes => attributes.AsSpan();

        /// <summary>
        /// Child XML nodes.
        /// </summary>
        public readonly ReadOnlySpan<XMLNode> Children => children.AsSpan();

        public readonly bool IsDisposed => name.IsDisposed;

        public XMLNode()
        {
            name = new();
            attributes = new();
            content = new();
            children = new();
        }

        public XMLNode(ref XMLReader reader)
        {
            ReadFrom(ref reader);
        }

        public XMLNode(ReadOnlySpan<byte> data)
        {
            XMLReader reader = new(data);
            ReadFrom(ref reader);
            reader.Dispose();
        }

        public unsafe XMLNode(ReadOnlySpan<char> data)
        {
            fixed (char* ptr = data)
            {
                Span<byte> bytes = new(ptr, data.Length * sizeof(char));
                XMLReader reader = new(bytes);
                ReadFrom(ref reader);
                reader.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (XMLNode child in children)
            {
                child.Dispose();
            }

            foreach (XMLAttribute attribute in attributes)
            {
                attribute.Dispose();
            }

            children.Dispose();
            content.Dispose();
            attributes.Dispose();
            name.Dispose();
        }

        public readonly override string ToString()
        {
            UnmanagedList<char> list = new();
            ToString(list, "  ", true, true);
            string str = list.AsSpan().ToString();
            list.Dispose();
            return str;
        }

        void IBinaryObject.Write(BinaryWriter writer)
        {
            UnmanagedList<char> list = new();
            ToString(list);
            writer.WriteSpan<char>(list.AsSpan());
            list.Dispose();
        }

        void IBinaryObject.Read(ref BinaryReader reader)
        {
            XMLReader xmlReader = new(reader);
            ReadFrom(ref xmlReader);
        }

        public readonly void ToString(UnmanagedList<char> list, ReadOnlySpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
        {
            for (byte i = 0; i < depth; i++)
            {
                Indent(indent);
            }

            list.Add('<');
            list.AddRange(Name);
            for (uint i = 0; i < attributes.Count; i++)
            {
                list.Add(' ');
                XMLAttribute attribute = attributes[i];
                attribute.ToString(list);
            }

            if (content.Count > 0 || children.Count > 0)
            {
                list.Add('>');

                depth++;
                list.AddRange(Content);

                if (children.Count > 0)
                {
                    foreach (XMLNode child in children)
                    {
                        NewLine();
                        child.ToString(list, indent, cr, lf, depth);
                    }

                    NewLine();
                    for (byte i = 0; i < depth - 1; i++)
                    {
                        Indent(indent);
                    }
                }

                list.Add('<');
                list.Add('/');
                list.AddRange(Name);
                list.Add('>');
            }
            else
            {
                list.Add('/');
                list.Add('>');
            }

            void NewLine()
            {
                if (cr)
                {
                    list.Add('\r');
                }

                if (lf)
                {
                    list.Add('\n');
                }
            }

            void Indent(ReadOnlySpan<char> indent)
            {
                list.AddRange(indent);
            }
        }

        private void ReadFrom(ref XMLReader reader)
        {
            attributes = new();
            content = new();
            children = new();

            Token token = reader.ReadToken(); //<

            //read name
            token = reader.ReadToken();
            name = new(reader.GetText(token));

            //read attributes inside first node
            while (reader.PeekToken(out token))
            {
                if (token.type == Token.Type.Close)
                {
                    token = reader.ReadToken();
                    break; //exit first node (assume there will be a closing node)
                }
                else if (token.type == Token.Type.Slash)
                {
                    token = reader.ReadToken();
                    token = reader.ReadToken();
                    if (token.type == Token.Type.Close)
                    {
                        return;
                    }

                    throw new Exception($"Unexpected token {token.type} after '/' when reading end of node attributes");
                }
                else
                {
                    ReadOnlySpan<char> value = reader.ReadAttribute(out ReadOnlySpan<char> name);
                    XMLAttribute attribute = new(name, value);
                    attributes.Add(attribute);
                }
            }

            //read content
            while (reader.PeekToken(out token))
            {
                if (token.type == Token.Type.Text)
                {
                    reader.ReadToken();
                    ReadOnlySpan<char> contentSpan = reader.GetText(token);
                    content.AddRange(contentSpan);
                }
                else if (token.type == Token.Type.Open)
                {
                    XMLReader closeReader = reader;
                    closeReader.ReadToken();
                    Token next = closeReader.ReadToken();
                    if (next.type == Token.Type.Slash)
                    {
                        next = closeReader.ReadToken();
                        if (next.type == Token.Type.Text)
                        {
                            ReadOnlySpan<char> closingName = closeReader.GetText(next);
                            if (closingName.SequenceEqual(Name))
                            {
                                reader.Position = closeReader.Position;
                                reader.ReadToken();
                                return;
                            }
                            else
                            {
                                throw new Exception($"Unexpected closing node {closingName.ToString()} when reading node {Name.ToString()}");
                            }
                        }
                        else
                        {
                            throw new Exception($"Unexpected token {next.type} when reading closing node");
                        }
                    }

                    XMLNode child = new(ref reader);
                    children.Add(child);
                }
                else
                {
                    throw new Exception($"Unexpected token {token.type} when reading content inside a node");
                }
            }
        }

        public readonly bool TryGetFirst(ReadOnlySpan<char> name, out XMLNode child)
        {
            foreach (XMLNode node in children)
            {
                if (node.Name.SequenceEqual(name))
                {
                    child = node;
                    return true;
                }
            }

            child = default;
            return false;
        }
    }
}