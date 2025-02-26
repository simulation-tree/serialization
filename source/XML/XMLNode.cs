using Collections;
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
        public readonly USpan<char> Name
        {
            get => name.AsSpan();
            set => name.CopyFrom(value);
        }

        /// <summary>
        /// Possible text content inside the node.
        /// </summary>
        public readonly USpan<char> Content
        {
            get => content.AsSpan();
            set => content.CopyFrom(value);
        }

        public readonly bool IsPrologue => prologue;

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
            name = new(4);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }
#endif

        public XMLNode(USpan<char> name)
        {
            this.name = new(name);
            attributes = new(4);
            content = new(0);
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
            ToString(buffer, "  ".AsSpan(), true, true);
            string str = buffer.AsSpan().ToString();
            buffer.Dispose();
            return str;
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            Text buffer = new(0);
            ToString(buffer);
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
                    uint valueLength = xmlReader.GetText(token, valueBuffer);
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
                                USpan<char> closingName = nameBuffer.Slice(0, xmlReader.GetText(closeToken, nameBuffer));
                                if (closingName.SequenceEqual(Name))
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
                        USpan<char> tempSpan = temp.AsSpan();
                        uint written = reader.PeekUTF8(token.position, token.length, tempSpan);
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
                                USpan<char> closingName = nameBuffer.Slice(0, length);
                                if (closingName.SequenceEqual(Name))
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

        public readonly void ToString(Text destination, USpan<char> indent = default, bool cr = false, bool lf = false, byte depth = 0)
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
            for (uint i = 0; i < attributes.Count; i++)
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
                        NewLine();
                        child.ToString(destination, indent, cr, lf, depth);
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
                if (cr)
                {
                    destination.Append('\r');
                }

                if (lf)
                {
                    destination.Append('\n');
                }
            }

            void Indent(USpan<char> indent)
            {
                destination.Append(indent);
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
            return children.TryRemoveBySwapping(node);
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
            return GetFirst(name.AsSpan());
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
            return TryGetFirst(name.AsSpan(), out child);
        }

        public readonly USpan<char> GetAttribute(string name)
        {
            return GetAttribute(name.AsSpan());
        }

        public readonly USpan<char> GetAttribute(USpan<char> name)
        {
            for (uint i = 0; i < attributes.Count; i++)
            {
                XMLAttribute attribute = attributes[i];
                if (attribute.Name.SequenceEqual(name))
                {
                    return attribute.Value;
                }
            }

            throw new NullReferenceException($"No attribute {name.ToString()} found");
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
            return TryGetAttribute(name.AsSpan(), out value);
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
            return ContainsAttribute(name.AsSpan());
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
            return IndexOfAttribute(name.AsSpan());
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
            return SetAttribute(name.AsSpan(), value);
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
            return RemoveAttribute(name.AsSpan());
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