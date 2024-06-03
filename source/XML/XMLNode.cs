using System;
using Unmanaged.Collections;

namespace Unmanaged.XML
{
    public struct XMLNode : IDisposable, ISerializable, IEquatable<XMLNode>
    {
        private UnmanagedArray<char> name;
        private UnmanagedList<XMLAttribute> attributes;
        private UnmanagedList<char> content;
        private UnmanagedList<XMLNode> children;

        /// <summary>
        /// Name of the node.
        /// </summary>
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

        public readonly ref XMLNode this[uint index]
        {
            get
            {
                if (index >= children.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref children.GetRef(index);
            }
        }

        public readonly ReadOnlySpan<char> this[ReadOnlySpan<char> name]
        {
            get
            {
                if (TryIndexOfAttribute(name, out uint index))
                {
                    return attributes[index].Value;
                }
                else throw new NullReferenceException($"No attribute {name.ToString()} found");
            }
            set
            {
                for (uint i = 0; i < attributes.Count; i++)
                {
                    XMLAttribute attribute = attributes[i];
                    if (attribute.Name.SequenceEqual(name))
                    {
                        attribute.Value = value;
                    }
                }

                throw new NullReferenceException($"No attribute {name.ToString()} found");
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

        public readonly uint Count => children.Count;

        public readonly bool IsDisposed => name.IsDisposed;

        public XMLNode()
        {
            name = new();
            attributes = new();
            content = new();
            children = new();
        }

        public XMLNode(ReadOnlySpan<char> name)
        {
            this.name = new(name);
            attributes = new();
            content = new();
            children = new();
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

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            UnmanagedList<char> list = new();
            ToString(list);
            writer.WriteSpan<char>(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(BinaryReader reader)
        {
            attributes = new();
            content = new();
            children = new();

            XMLReader xmlReader = new(reader);
            Token token = xmlReader.ReadToken(); //<

            //read name
            token = xmlReader.ReadToken();
            Span<char> nameBuffer = stackalloc char[256];
            Span<char> valueBuffer = stackalloc char[256];
            int length = xmlReader.GetText(token, nameBuffer);
            name = new(nameBuffer[..length]);

            //read attributes inside first node
            while (xmlReader.ReadToken(out token))
            {
                if (token.type == Token.Type.Close)
                {
                    break; //exit first node (assume there will be a closing node)
                }
                else if (token.type == Token.Type.Slash)
                {
                    token = xmlReader.ReadToken();
                    if (token.type == Token.Type.Close)
                    {
                        return;
                    }

                    throw new Exception($"Unexpected token {token.type} after '/' when reading end of node attributes");
                }
                else
                {
                    length = xmlReader.GetText(token, nameBuffer);
                    token = xmlReader.ReadToken();
                    int valueLength = xmlReader.GetText(token, valueBuffer);
                    XMLAttribute attribute = new(nameBuffer[..length], valueBuffer[..valueLength]);
                    attributes.Add(attribute);
                }
            }

            //read content
            while (xmlReader.ReadToken(out token))
            {
                if (token.type == Token.Type.Text || token.type == Token.Type.Open)
                {
                    if (token.type == Token.Type.Open)
                    {
                        reader.Position -= token.length;
                        XMLNode child = xmlReader.ReadNode();
                        children.Add(child);
                    }
                    else
                    {
                        using UnmanagedArray<char> temp = new(token.length);
                        Span<char> tempSpan = temp.AsSpan();
                        int written = reader.PeekUTF8Span(token.position, token.length, tempSpan);
                        content.AddRange(tempSpan[..written]);
                        reader.Position = token.position + token.length;
                    }

                    if (xmlReader.PeekToken(out Token next) && next.type == Token.Type.Open)
                    {
                        xmlReader.PeekToken(next.position + next.length, out next);
                        if (next.type == Token.Type.Slash)
                        {
                            xmlReader.ReadToken(); //open
                            xmlReader.ReadToken(); //slash
                            if (xmlReader.ReadToken(out next) && next.type == Token.Type.Text)
                            {
                                length = xmlReader.GetText(next, nameBuffer);
                                ReadOnlySpan<char> closingName = nameBuffer[..length];
                                if (closingName.SequenceEqual(Name))
                                {
                                    next = xmlReader.ReadToken(); //close
                                    if (next.type != Token.Type.Close)
                                    {
                                        throw new Exception($"Unexpected token {next.type} when reading closing node {closingName.ToString()}");
                                    }

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
                    }
                }
                else
                {
                    throw new Exception($"Unexpected token {token.type} when reading content inside a node");
                }
            }
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

        public readonly void Add(XMLNode child)
        {
            children.Add(child);
        }

        public readonly void RemoveAt(uint index)
        {
            children.RemoveAtBySwapping(index);
        }

        public readonly bool Remove(XMLNode node)
        {
            return children.TryRemove(node);
        }

        public readonly uint IndexOf(XMLNode node)
        {
            return children.IndexOf(node);
        }

        public readonly bool TryIndexOf(XMLNode node, out uint index)
        {
            return children.TryIndexOf(node, out index);
        }

        public readonly XMLNode GetFirst(ReadOnlySpan<char> name)
        {
            foreach (XMLNode node in children)
            {
                if (node.Name.SequenceEqual(name))
                {
                    return node;
                }
            }

            throw new NullReferenceException($"No child node {name.ToString()} found");
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

        public readonly bool TryGetAttribute(ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    value = attribute.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public readonly bool TryIndexOfAttribute(ReadOnlySpan<char> name, out uint index)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        public readonly bool ContainsAttribute(ReadOnlySpan<char> name)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly uint IndexOfAttribute(ReadOnlySpan<char> name)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    return i;
                }
            }

            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Creates a new attribute or assigns an existing one to the given value.
        /// </summary>
        /// <returns><c>true</c> if it was created, otherwise it was set</returns>
        public readonly bool SetAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    attribute.Value = value;
                    return false;
                }
            }

            XMLAttribute newAttribute = new(name, value);
            attributes.Add(newAttribute);
            return true;
        }

        public readonly bool RemoveAttribute(ReadOnlySpan<char> name)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    attributes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is XMLNode node && Equals(node);
        }

        public readonly bool Equals(XMLNode other)
        {
            return name.Equals(other.name) && attributes.Equals(other.attributes) && content.Equals(other.content) && children.Equals(other.children);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(name, attributes, content, children);
        }

        public static bool operator ==(XMLNode left, XMLNode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(XMLNode left, XMLNode right)
        {
            return !(left == right);
        }
    }
}