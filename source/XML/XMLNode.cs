using Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.XML
{
    public struct XMLNode : IDisposable, ISerializable, IEquatable<XMLNode>
    {
        private Text name;
        private List<XMLAttribute> attributes;
        private Text content;
        private List<XMLNode> children;
        private bool prologue;

        /// <summary>
        /// Name of the node.
        /// </summary>
        public readonly Text.Borrowed Name => name.Borrow();

        /// <summary>
        /// Possible text content inside the node.
        /// </summary>
        public readonly Text.Borrowed Content => content.Borrow();

        public readonly bool IsPrologue => prologue;

        public readonly ref XMLNode this[int index]
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

        public readonly ReadOnlySpan<char> this[ReadOnlySpan<char> name]
        {
            get
            {
                if (TryIndexOfAttribute(name, out int index))
                {
                    return attributes[index].Value.AsSpan();
                }
                else throw new NullReferenceException($"No attribute {name.ToString()} found");
            }
            set
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    XMLAttribute attribute = attributes[i];
                    if (attribute.Name.Equals(name))
                    {
                        attribute.Value.CopyFrom(value);
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

        public readonly int Count => children.Count;

        public readonly bool IsDisposed => name.IsDisposed;

#if NET
        public XMLNode()
        {
            name = new(4);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }
#endif
        public XMLNode(string name)
        {
            this.name = new(name);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }

        public XMLNode(ReadOnlySpan<char> name)
        {
            this.name = new(name);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }

        public XMLNode(string name, string content)
        {
            this.name = new(name);
            attributes = new(4);
            this.content = new(content);
            children = new(4);
        }

        public XMLNode(ReadOnlySpan<char> name, ReadOnlySpan<char> content)
        {
            this.name = new(name);
            attributes = new(4);
            this.content = new(content);
            children = new(4);
        }

        private XMLNode(Text name, List<XMLAttribute> attributes, Text content, List<XMLNode> children, bool prologue)
        {
            this.name = name;
            this.attributes = attributes;
            this.content = content;
            this.children = children;
            this.prologue = prologue;
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
            Text buffer = new(0);
            ToStringFlags flags = ToStringFlags.CarriageReturn | ToStringFlags.LineFeed;
            ToString(buffer, "  ", flags);
            string str = buffer.AsSpan().ToString();
            buffer.Dispose();
            return str;
        }

        public readonly string ToString(string indent, ToStringFlags flags = default)
        {
            Text buffer = new(0);
            ToString(buffer, indent, flags);
            string str = buffer.AsSpan().ToString();
            buffer.Dispose();
            return str;
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            Text buffer = new(0);
            ToStringFlags flags = ToStringFlags.CarriageReturn | ToStringFlags.LineFeed;
            ToString(buffer, default, flags, 0);
            writer.WriteSpan(buffer.AsSpan());
            buffer.Dispose();
        }

        [SkipLocalsInit]
        void ISerializable.Read(ByteReader reader)
        {
            attributes = new(4);
            content = new(0);
            children = new(4);

            XMLReader xmlReader = new(reader);
            Token token = xmlReader.ReadToken(); //<

            if (xmlReader.PeekToken(out Token nextToken) && nextToken.type == Token.Type.Prologue)
            {
                prologue = true;
                xmlReader.ReadToken();
            }

            //read name
            token = xmlReader.ReadToken();
            Span<char> nameBuffer = stackalloc char[256];
            Span<char> valueBuffer = stackalloc char[256];
            int length = xmlReader.GetText(token, nameBuffer);
            name = new(nameBuffer.Slice(0, length));

            //read attributes inside first node
            while (xmlReader.ReadToken(out token))
            {
                if (token.type == Token.Type.Close)
                {
                    break; //exit first node (assume there will be a closing node)
                }
                else if (token.type == Token.Type.Prologue)
                {
                    continue;
                }
                else if (token.type == Token.Type.Slash)
                {
                    token = xmlReader.ReadToken();
                    if (token.type == Token.Type.Close)
                    {
                        return;
                    }

                    throw new Exception($"Unexpected token `{token.type}` after '/' when reading end of node attributes");
                }
                else
                {
                    length = xmlReader.GetText(token, nameBuffer);
                    token = xmlReader.ReadToken();
                    int valueLength = xmlReader.GetText(token, valueBuffer);
                    XMLAttribute attribute = new(nameBuffer.Slice(0, length), valueBuffer.Slice(0, valueLength));
                    attributes.Add(attribute);
                }
            }

            if (token.type != Token.Type.Close && token.type != Token.Type.Unknown)
            {
                throw new Exception($"Unexpected token `{token.type}` when reading end of node attributes");
            }

            //read content
            while (xmlReader.ReadToken(out token))
            {
                if (token.type == Token.Type.Text || token.type == Token.Type.Open)
                {
                    if (token.type == Token.Type.Open)
                    {
                        //check if this open node closes itself
                        if (xmlReader.PeekToken(out Token closeToken) && closeToken.type == Token.Type.Slash)
                        {
                            if (xmlReader.PeekToken(closeToken.position + closeToken.length, out closeToken) && closeToken.type == Token.Type.Text)
                            {
                                ReadOnlySpan<char> closingName = nameBuffer.Slice(0, xmlReader.GetText(closeToken, nameBuffer));
                                if (name.Equals(closingName))
                                {
                                    xmlReader.ReadToken(); //open
                                    xmlReader.ReadToken(); //slash
                                    xmlReader.ReadToken(); //close
                                    return;
                                }
                                else
                                {
                                    throw new Exception($"Encountered closing node `{closingName.ToString()}` while reading `{Name.ToString()}`");
                                }
                            }
                        }

                        reader.Position -= token.length;
                        XMLNode child = xmlReader.ReadNode();
                        children.Add(child);
                    }
                    else
                    {
                        using Text temp = new(token.length);
                        Span<char> tempSpan = temp.AsSpan();
                        int written = reader.PeekUTF8(token.position, token.length, tempSpan);
                        content.Append(tempSpan.Slice(0, written));
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
                                ReadOnlySpan<char> closingName = nameBuffer.Slice(0, length);
                                if (name.Equals(closingName))
                                {
                                    next = xmlReader.ReadToken(); //close
                                    if (next.type != Token.Type.Close)
                                    {
                                        throw new Exception($"Unexpected token `{next.type}` when reading closing node `{closingName.ToString()}`");
                                    }

                                    return;
                                }
                                else
                                {
                                    throw new Exception($"Unexpected closing node `{closingName.ToString()}` when reading node `{Name.ToString()}`");
                                }
                            }
                            else
                            {
                                throw new Exception($"Unexpected token `{next.type}` when reading closing node");
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unexpected token `{token.type}` when reading content inside a node");
                }
            }
        }

        public readonly void ToString(Text destination, ReadOnlySpan<char> indent = default, ToStringFlags flags = default)
        {
            ToString(destination, indent, flags, 0);
        }

        public readonly void ToString(Text destination, string indent = "", ToStringFlags flags = default)
        {
            ToString(destination, indent.AsSpan(), flags, 0);
        }

        private readonly void ToString(Text destination, ReadOnlySpan<char> indent, ToStringFlags flags, byte depth)
        {
            for (int i = 0; i < depth; i++)
            {
                Indent(indent);
            }

            destination.Append('<');
            if (prologue)
            {
                destination.Append('?');
            }

            destination.Append(Name);
            for (int i = 0; i < attributes.Count; i++)
            {
                destination.Append(' ');
                XMLAttribute attribute = attributes[i];
                attribute.ToString(destination);
            }

            if (content.Length > 0 || children.Count > 0)
            {
                if (prologue)
                {
                    destination.Append('?');
                }
                else
                {
                    depth++;
                }

                destination.Append('>');
                destination.Append(Content);

                if (children.Count > 0)
                {
                    foreach (XMLNode child in children)
                    {
                        if (depth == 1 && (flags & ToStringFlags.RootSpacing) == ToStringFlags.RootSpacing)
                        {
                            NewLine();
                        }

                        NewLine();
                        child.ToString(destination, indent, flags, depth);
                    }


                    if (depth == 1 && (flags & ToStringFlags.RootSpacing) == ToStringFlags.RootSpacing)
                    {
                        NewLine();
                    }

                    NewLine();
                    for (int i = 0; i < depth - 1; i++)
                    {
                        Indent(indent);
                    }
                }

                if (!prologue)
                {
                    destination.Append('<');
                    destination.Append('/');
                    destination.Append(Name);
                    destination.Append('>');
                }
            }
            else
            {
                destination.Append('/');
                destination.Append('>');
            }

            void NewLine()
            {
                if ((flags & ToStringFlags.CarriageReturn) == ToStringFlags.CarriageReturn)
                {
                    destination.Append('\r');
                }

                if ((flags & ToStringFlags.LineFeed) == ToStringFlags.LineFeed)
                {
                    destination.Append('\n');
                }
            }

            void Indent(ReadOnlySpan<char> indent)
            {
                destination.Append(indent);
            }
        }

        /// <summary>
        /// Adds the given <paramref name="child"/> node to the list of children.
        /// </summary>
        public readonly void Add(XMLNode child)
        {
            children.Add(child);
        }

        public readonly void RemoveAt(int index)
        {
            children.RemoveAtBySwapping(index);
        }

        public readonly bool Remove(XMLNode node)
        {
            return children.TryRemoveBySwapping(node);
        }

        public readonly int IndexOf(XMLNode node)
        {
            return children.IndexOf(node);
        }

        public readonly bool TryIndexOf(XMLNode node, out int index)
        {
            return children.TryIndexOf(node, out index);
        }

        public readonly XMLNode GetFirst(ReadOnlySpan<char> name)
        {
            foreach (XMLNode node in children)
            {
                if (node.Name.Equals(name))
                {
                    return node;
                }
            }

            throw new NullReferenceException($"No child node {name.ToString()} found");
        }

        public readonly XMLNode GetFirst(string name)
        {
            return GetFirst(name.AsSpan());
        }

        public readonly bool TryGetFirst(ReadOnlySpan<char> name, out XMLNode child)
        {
            foreach (XMLNode node in children)
            {
                if (node.Name.Equals(name))
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
            return TryGetFirst(name.AsSpan(), out child);
        }

        public readonly ReadOnlySpan<char> GetAttribute(string name)
        {
            return GetAttribute(name.AsSpan());
        }

        public readonly ReadOnlySpan<char> GetAttribute(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
                {
                    return attribute.Value.AsSpan();
                }
            }

            throw new NullReferenceException($"No attribute {name.ToString()} found");
        }

        public readonly bool TryGetAttribute(ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
                {
                    value = attribute.Value.AsSpan();
                    return true;
                }
            }

            value = default;
            return false;
        }

        public readonly bool TryGetAttribute(string name, out ReadOnlySpan<char> value)
        {
            return TryGetAttribute(name.AsSpan(), out value);
        }

        public readonly bool TryIndexOfAttribute(ReadOnlySpan<char> name, out int index)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
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
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool ContainsAttribute(string name)
        {
            return ContainsAttribute(name.AsSpan());
        }

        public readonly int IndexOfAttribute(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
                {
                    return i;
                }
            }

            return -1;
        }

        public readonly int IndexOfAttribute(string name)
        {
            return IndexOfAttribute(name.AsSpan());
        }

        /// <summary>
        /// Creates a new attribute or assigns an existing one to the given value.
        /// </summary>
        /// <returns><c>true</c> if it was created, otherwise it was set</returns>
        public readonly bool SetAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
                {
                    attribute.Value.CopyFrom(value);
                    return false;
                }
            }

            XMLAttribute newAttribute = new(name, value);
            attributes.Add(newAttribute);
            return true;
        }

        public readonly bool SetAttribute(string name, ReadOnlySpan<char> value)
        {
            return SetAttribute(name.AsSpan(), value);
        }

        public readonly bool RemoveAttribute(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.Equals(name))
                {
                    attributes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public readonly bool RemoveAttribute(string name)
        {
            return RemoveAttribute(name.AsSpan());
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is XMLNode node && Equals(node);
        }

        public readonly bool Equals(XMLNode other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }
            else if (IsDisposed != other.IsDisposed)
            {
                return false;
            }

            return name.Equals(other.name) && attributes.Equals(other.attributes) && content.Equals(other.content) && children.Equals(other.children);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(name, attributes, content, children);
        }

        public static XMLNode Create(ReadOnlySpan<char> name)
        {
            return new XMLNode(name);
        }

        public static XMLNode Create(string name)
        {
            return Create(name.AsSpan());
        }

        public static XMLNode Create()
        {
            Text name = new(4);
            List<XMLAttribute> attributes = new(4);
            Text content = new(0);
            List<XMLNode> children = new(4);
            return new XMLNode(name, attributes, content, children, false);
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