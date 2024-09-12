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
        public readonly USpan<char> Name
        {
            get => name.AsSpan();
            set
            {
                uint newLength = (uint)value.length;
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
        public readonly USpan<char> Content
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

                return ref children[index];
            }
        }

        public readonly USpan<char> this[USpan<char> name]
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
        public readonly USpan<XMLAttribute> Attributes => attributes.AsSpan();

        /// <summary>
        /// Child XML nodes.
        /// </summary>
        public readonly USpan<XMLNode> Children => children.AsSpan();

        public readonly uint Count => children.Count;

        public readonly bool IsDisposed => name.IsDisposed;

#if NET
        public XMLNode()
        {
            name = UnmanagedArray<char>.Create();
            attributes = UnmanagedList<XMLAttribute>.Create();
            content = UnmanagedList<char>.Create();
            children = UnmanagedList<XMLNode>.Create();
        }
#endif

        public XMLNode(USpan<char> name)
        {
            this.name = new(name);
            attributes = UnmanagedList<XMLAttribute>.Create();
            content = UnmanagedList<char>.Create();
            children = UnmanagedList<XMLNode>.Create();
        }

        private XMLNode(UnmanagedArray<char> name, UnmanagedList<XMLAttribute> attributes, UnmanagedList<char> content, UnmanagedList<XMLNode> children)
        {
            this.name = name;
            this.attributes = attributes;
            this.content = content;
            this.children = children;
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
            UnmanagedList<char> list = UnmanagedList<char>.Create();
            ToString(list, "  ".AsUSpan(), true, true);
            string str = list.AsSpan().ToString();
            list.Dispose();
            return str;
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            UnmanagedList<char> list = UnmanagedList<char>.Create();
            ToString(list);
            writer.WriteSpan<char>(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(BinaryReader reader)
        {
            attributes = UnmanagedList<XMLAttribute>.Create();
            content = UnmanagedList<char>.Create();
            children = UnmanagedList<XMLNode>.Create();

            XMLReader xmlReader = new(reader);
            Token token = xmlReader.ReadToken(); //<

            //read name
            token = xmlReader.ReadToken();
            USpan<char> nameBuffer = stackalloc char[256];
            USpan<char> valueBuffer = stackalloc char[256];
            uint length = xmlReader.GetText(token, nameBuffer);
            name = new(nameBuffer.Slice(0, length));

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
                    uint valueLength = xmlReader.GetText(token, valueBuffer);
                    XMLAttribute attribute = new(nameBuffer.Slice(0, length), valueBuffer.Slice(0, valueLength));
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
                        USpan<char> tempSpan = temp.AsSpan();
                        uint written = reader.PeekUTF8Span(token.position, token.length, tempSpan);
                        content.AddRange(tempSpan.Slice(0, written));
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
                                USpan<char> closingName = nameBuffer.Slice(0, length);
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

        public readonly void ToString(UnmanagedList<char> list, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
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

            void Indent(USpan<char> indent)
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

        public readonly XMLNode GetFirst(USpan<char> name)
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

        public readonly XMLNode GetFirst(string name)
        {
            return GetFirst(name.AsUSpan());
        }

        public readonly bool TryGetFirst(USpan<char> name, out XMLNode child)
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

        public readonly bool TryGetFirst(string name, out XMLNode child)
        {
            return TryGetFirst(name.AsUSpan(), out child);
        }

        public readonly bool TryGetAttribute(USpan<char> name, out USpan<char> value)
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

        public readonly bool TryGetAttribute(string name, out USpan<char> value)
        {
            return TryGetAttribute(name.AsUSpan(), out value);
        }

        public readonly bool TryIndexOfAttribute(USpan<char> name, out uint index)
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

        public readonly bool ContainsAttribute(USpan<char> name)
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

        public readonly bool ContainsAttribute(string name)
        {
            return ContainsAttribute(name.AsUSpan());
        }

        public readonly uint IndexOfAttribute(USpan<char> name)
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

        public readonly uint IndexOfAttribute(string name)
        {
            return IndexOfAttribute(name.AsUSpan());
        }

        /// <summary>
        /// Creates a new attribute or assigns an existing one to the given value.
        /// </summary>
        /// <returns><c>true</c> if it was created, otherwise it was set</returns>
        public readonly bool SetAttribute(USpan<char> name, USpan<char> value)
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

        public readonly bool SetAttribute(string name, USpan<char> value)
        {
            return SetAttribute(name.AsUSpan(), value);
        }

        public readonly bool RemoveAttribute(USpan<char> name)
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

        public readonly bool RemoveAttribute(string name)
        {
            return RemoveAttribute(name.AsUSpan());
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

        public static XMLNode Create(USpan<char> name)
        {
            return new XMLNode(name);
        }

        public static XMLNode Create(string name)
        {
            return Create(name.AsUSpan());
        }

        public static XMLNode Create()
        {
            UnmanagedArray<char> name = UnmanagedArray<char>.Create();
            UnmanagedList<XMLAttribute> attributes = UnmanagedList<XMLAttribute>.Create();
            UnmanagedList<char> content = UnmanagedList<char>.Create();
            UnmanagedList<XMLNode> children = UnmanagedList<XMLNode>.Create();
            return new XMLNode(name, attributes, content, children);
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